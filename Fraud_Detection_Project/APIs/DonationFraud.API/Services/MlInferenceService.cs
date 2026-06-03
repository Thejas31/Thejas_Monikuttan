using DonationFraud.API.Data;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using DonationFraud.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace DonationFraud.API.Services
{
    public class MlInferenceService : IMlInferenceService
    {
        private readonly DonationDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MlInferenceService> _logger;

        private static readonly Dictionary<string, (double Lat, double Lon)> CountryCoordinates = new Dictionary<string, (double, double)>(StringComparer.OrdinalIgnoreCase)
        {
            { "IN", (20.5937, 78.9629) },
            { "US", (37.0902, -95.7129) },
            { "GB", (55.3781, -3.4360) },
            { "CA", (56.1304, -106.3468) },
            { "DE", (51.1657, 10.4515) },
            { "AU", (-25.2744, 133.7751) },
            { "FR", (46.2276, 2.2137) },
            { "SG", (1.3521, 103.8198) }
        };

        public MlInferenceService(
            DonationDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<MlInferenceService> _logger)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            this._logger = _logger;
        }

        public async Task<MlPredictionResult> GetMlPredictionAsync(Donation donation, int userId)
        {
            // 1. Feature Extraction: Velocity count (last 5 minutes from same IP)
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
            var ipCount = await _dbContext.Donations
                .CountAsync(d => d.IpAddress == donation.IpAddress && d.Timestamp >= fiveMinutesAgo);

            // Velocity count (last 10 minutes attempts by same User)
            var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);
            var userAttempts = await _dbContext.Donations
                .CountAsync(d => d.UserId == userId && d.Timestamp >= tenMinutesAgo);

            // Account age details
            var user = await _dbContext.Users.FindAsync(userId);
            var accountAgeDays = 0.0;
            if (user != null)
            {
                accountAgeDays = (DateTime.UtcNow - user.CreatedAt).TotalDays;
            }

            // Payment Card Velocity (unique card fingerprints used by User in the last hour)
            var paymentMethod = donation.PaymentMethodId.HasValue 
                ? await _dbContext.PaymentMethods.FindAsync(donation.PaymentMethodId.Value)
                : null;

            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var uniqueCardsCount = 0;
            if (paymentMethod != null)
            {
                uniqueCardsCount = await _dbContext.Donations
                    .Where(d => d.UserId == userId && d.Timestamp >= oneHourAgo && d.PaymentMethodId != null)
                    .Select(d => d.PaymentMethod!.Fingerprint)
                    .Distinct()
                    .CountAsync();
            }

            // Geolocation and IP Mismatch Calculations
            var ipIntelligence = donation.IpIntelligenceId.HasValue
                ? await _dbContext.IpIntelligences.FindAsync(donation.IpIntelligenceId.Value)
                : null;

            var distanceKm = 0.0;
            if (ipIntelligence != null && paymentMethod != null && !string.IsNullOrEmpty(paymentMethod.BankCountryCode))
            {
                if (CountryCoordinates.TryGetValue(paymentMethod.BankCountryCode, out var cardCoords))
                {
                    distanceKm = CalculateHaversineDistance(ipIntelligence.Latitude, ipIntelligence.Longitude, cardCoords.Lat, cardCoords.Lon);
                }
            }

            // Device Context Analysis (Screen OS mismatch checker)
            var deviceFingerprint = donation.DeviceFingerprintId.HasValue
                ? await _dbContext.DeviceFingerprints.FindAsync(donation.DeviceFingerprintId.Value)
                : null;

            var isScreenOsMismatch = false;
            if (deviceFingerprint != null)
            {
                var ua = deviceFingerprint.UserAgent.ToLower();
                var os = deviceFingerprint.Os.ToLower();
                if ((ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("android")) && os.Contains("windows"))
                {
                    isScreenOsMismatch = true;
                }
                else if (ua.Contains("windows") && (os.Contains("ios") || os.Contains("android")))
                {
                    isScreenOsMismatch = true;
                }
            }

            // Amount Ratio comparison
            var averageDonation = await _dbContext.Donations
                .Where(d => d.CampaignId == donation.CampaignId)
                .Select(d => (double)d.Amount)
                .DefaultIfEmpty(0.0)
                .AverageAsync();

            var amountRatio = averageDonation > 0 ? (double)donation.Amount / averageDonation : 1.0;

            // Compile engineered feature vector
            var features = new Dictionary<string, object>
            {
                { "amount", (double)donation.Amount },
                { "ip_count_5m", ipCount },
                { "user_attempts_10m", userAttempts },
                { "account_age_days", accountAgeDays },
                { "card_count_1h", uniqueCardsCount },
                { "distance_ip_to_card_km", distanceKm },
                { "is_vpn_proxy", ipIntelligence?.IsVpnOrProxy ?? false },
                { "screen_os_mismatch", isScreenOsMismatch },
                { "amount_ratio_campaign_avg", amountRatio }
            };

            var featuresJson = JsonSerializer.Serialize(features);
            _logger.LogInformation("Extracted ML Features for Donation {DonationId}: {Features}", donation.Id, featuresJson);

            // 2. HTTP Request to FastAPI Python Microservice (will gracefully failover if not running)
            try
            {
                var mlServiceUrl = _configuration["MlService:BaseUrl"];
                if (!string.IsNullOrEmpty(mlServiceUrl))
                {
                    var client = _httpClientFactory.CreateClient("MlService");
                    client.Timeout = TimeSpan.FromMilliseconds(800); // Quick timeout to prevent UI lag

                    var response = await client.PostAsJsonAsync($"{mlServiceUrl}/predict", features);
                    if (response.IsSuccessStatusCode)
                    {
                        var prediction = await response.Content.ReadFromJsonAsync<MlPredictionResult>();
                        if (prediction != null)
                        {
                            _logger.LogInformation("Received real-time AI Prediction from ML Microservice: RiskScore={Score}", prediction.RiskScore);
                            return prediction;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to fetch prediction from ML Microservice (Fallback activated): {Message}", ex.Message);
            }

            // 3. Fallback: Simulated ML model output based on rules
            int simulatedRiskScore = 0;
            var impactDict = new Dictionary<string, double>();

            if (ipIntelligence != null && ipIntelligence.IsVpnOrProxy)
            {
                simulatedRiskScore += 25;
                impactDict.Add("is_vpn_proxy", +0.25);
            }
            if (uniqueCardsCount > 2)
            {
                simulatedRiskScore += 30;
                impactDict.Add("card_count_1h", +0.30);
            }
            if (amountRatio > 5.0)
            {
                simulatedRiskScore += 20;
                impactDict.Add("amount_ratio_campaign_avg", +0.20);
            }
            if (isScreenOsMismatch)
            {
                simulatedRiskScore += 15;
                impactDict.Add("screen_os_mismatch", +0.15);
            }
            if (accountAgeDays < 1.0)
            {
                simulatedRiskScore += 10;
                impactDict.Add("account_age_days", +0.10);
            }

            simulatedRiskScore = Math.Min(100, simulatedRiskScore);
            string simulatedRiskLevel = simulatedRiskScore >= 70 ? "High" : (simulatedRiskScore >= 30 ? "Medium" : "Low");
            string simulatedImpactJson = JsonSerializer.Serialize(impactDict);

            return new MlPredictionResult
            {
                RiskScore = simulatedRiskScore,
                RiskLevel = simulatedRiskLevel,
                ModelVersion = "Mock_v1-rules",
                TopFeaturesImpact = simulatedImpactJson
            };
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double val)
        {
            return (Math.PI / 180) * val;
        }
    }
}
