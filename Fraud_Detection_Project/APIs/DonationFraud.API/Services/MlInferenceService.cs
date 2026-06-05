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

            // Feature translation calculation for XGBoost
            double card1 = paymentMethod != null ? Math.Abs(paymentMethod.Fingerprint.GetHashCode()) % 100000 : -999.0;
            
            double card2 = -999.0;
            if (paymentMethod != null)
            {
                var brand = paymentMethod.CardBrand.ToLower();
                if (brand.Contains("visa")) card2 = 111.0;
                else if (brand.Contains("mastercard") || brand.Contains("master")) card2 = 222.0;
                else if (brand.Contains("amex") || brand.Contains("american")) card2 = 333.0;
                else if (brand.Contains("discover")) card2 = 444.0;
                else card2 = Math.Abs(paymentMethod.CardBrand.GetHashCode()) % 1000;
            }

            double card3 = paymentMethod != null && !string.IsNullOrEmpty(paymentMethod.BankCountryCode) 
                ? Math.Abs(paymentMethod.BankCountryCode.GetHashCode()) % 1000 
                : -999.0;

            double card5 = paymentMethod != null ? (paymentMethod.ThreeDSecureSuccess ? 100.0 : 0.0) : -999.0;

            double addr1 = ipIntelligence != null && !string.IsNullOrEmpty(ipIntelligence.City) 
                ? Math.Abs(ipIntelligence.City.GetHashCode()) % 1000 
                : -999.0;

            double addr2 = ipIntelligence != null && !string.IsNullOrEmpty(ipIntelligence.CountryCode) 
                ? Math.Abs(ipIntelligence.CountryCode.GetHashCode()) % 1000 
                : -999.0;

            double dist1 = distanceKm > 0 ? distanceKm : -999.0;

            double id01 = ipIntelligence != null && ipIntelligence.IsVpnOrProxy ? -100.0 : 0.0;
            double id02 = isScreenOsMismatch ? 999.0 : 0.0;

            double deviceTypeVal = -999.0;
            if (deviceFingerprint != null)
            {
                var type = deviceFingerprint.DeviceType.ToLower();
                if (type.Contains("desktop")) deviceTypeVal = 1.0;
                else if (type.Contains("mobile")) deviceTypeVal = 2.0;
                else deviceTypeVal = 3.0;
            }

            double deviceInfoVal = -999.0;
            if (deviceFingerprint != null)
            {
                var os = deviceFingerprint.Os.ToLower();
                if (os.Contains("windows")) deviceInfoVal = 1.0;
                else if (os.Contains("ios") || os.Contains("iphone") || os.Contains("ipad")) deviceInfoVal = 2.0;
                else if (os.Contains("android")) deviceInfoVal = 3.0;
                else if (os.Contains("mac") || os.Contains("osx")) deviceInfoVal = 4.0;
                else if (os.Contains("linux")) deviceInfoVal = 5.0;
                else deviceInfoVal = Math.Abs(deviceFingerprint.Os.GetHashCode()) % 50;
            }

            string emailDomain = "unknown";
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                var parts = user.Email.Split('@');
                if (parts.Length > 1)
                {
                    emailDomain = parts[1].ToLower();
                }
            }

            double pEmailDomainVal = -999.0;
            if (emailDomain != "unknown")
            {
                if (emailDomain.Contains("gmail")) pEmailDomainVal = 12.0;
                else if (emailDomain.Contains("yahoo")) pEmailDomainVal = 8.0;
                else if (emailDomain.Contains("hotmail") || emailDomain.Contains("outlook")) pEmailDomainVal = 15.0;
                else if (emailDomain.Contains("aol")) pEmailDomainVal = 3.0;
                else pEmailDomainVal = Math.Abs(emailDomain.GetHashCode()) % 100;
            }
            double rEmailDomainVal = pEmailDomainVal;

            // Compile 31-dimensional IEEE-CIS feature vector in exact order
            var features = new Dictionary<string, object>
            {
                { "TransactionAmt", (double)donation.Amount },
                { "card1", card1 },
                { "card2", card2 },
                { "card3", card3 },
                { "card5", card5 },
                { "addr1", addr1 },
                { "addr2", addr2 },
                { "dist1", dist1 },
                { "dist2", -999.0 },
                { "C1", (double)ipCount },
                { "C2", (double)userAttempts },
                { "C3", (double)uniqueCardsCount },
                { "C4", -999.0 },
                { "C5", -999.0 },
                { "D1", accountAgeDays },
                { "D2", -999.0 },
                { "D3", -999.0 },
                { "D4", -999.0 },
                { "D5", -999.0 },
                { "id_01", id01 },
                { "id_02", id02 },
                { "id_03", -999.0 },
                { "id_04", -999.0 },
                { "id_05", -999.0 },
                { "id_06", -999.0 },
                { "id_11", 100.0 },
                { "id_13", -999.0 },
                { "DeviceType", deviceTypeVal },
                { "DeviceInfo", deviceInfoVal },
                { "P_emaildomain", pEmailDomainVal },
                { "R_emaildomain", rEmailDomainVal }
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
                        var apiResponse = await response.Content.ReadFromJsonAsync<FastApiPredictionResponseDto>();
                        if (apiResponse != null)
                        {
                            _logger.LogInformation("Received real-time AI Prediction from ML Microservice: RiskScore={Score}", apiResponse.RiskScore);
                            return new MlPredictionResult
                            {
                                RiskScore = apiResponse.RiskScore,
                                RiskLevel = apiResponse.IsFraud ? "High" : (apiResponse.RiskScore >= 30 ? "Medium" : "Low"),
                                ModelVersion = "XGBoost_v1",
                                TopFeaturesImpact = JsonSerializer.Serialize(new Dictionary<string, double>
                                {
                                    { "probability", apiResponse.Probability }
                                })
                            };
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
                impactDict.Add("id_01", +0.25);
            }
            if (uniqueCardsCount > 2)
            {
                simulatedRiskScore += 30;
                impactDict.Add("C3", +0.30);
            }
            if (amountRatio > 5.0)
            {
                simulatedRiskScore += 20;
                impactDict.Add("TransactionAmt_ratio", +0.20);
            }
            if (isScreenOsMismatch)
            {
                simulatedRiskScore += 15;
                impactDict.Add("id_02", +0.15);
            }
            if (accountAgeDays < 1.0)
            {
                simulatedRiskScore += 10;
                impactDict.Add("D1", +0.10);
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
