using DonationFraud.API.Data;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using DonationFraud.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DonationFraud.Tests.Services
{
    public class MlInferenceServiceTests
    {
        private readonly DonationDbContext _context;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ILogger<MlInferenceService>> _loggerMock;
        private readonly MlInferenceService _service;

        public MlInferenceServiceTests()
        {
            var dbOptions = new DbContextOptionsBuilder<DonationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            _context = new DonationDbContext(dbOptions);

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<MlInferenceService>>();

            // Setup basic config
            _configMock.Setup(c => c["MlService:BaseUrl"]).Returns("http://localhost:8000");

            _service = new MlInferenceService(
                _context,
                _httpClientFactoryMock.Object,
                _configMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetMlPredictionAsync_TranslatesFeaturesAndInvokesFallback_WhenServiceOffline()
        {
            // Arrange
            var user = new User
            {
                Id = 10,
                Username = "testuser",
                Email = "testuser@gmail.com",
                CreatedAt = DateTime.UtcNow.AddDays(-5) // 5 days old account
            };
            var campaign = new Campaign
            {
                Id = 20,
                Title = "Save Wildlife",
                TargetAmount = 5000,
                IsActive = true
            };
            _context.Users.Add(user);
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            var donation = new Donation
            {
                Id = 100,
                Amount = 1500,
                IpAddress = "127.0.0.1",
                Timestamp = DateTime.UtcNow,
                UserId = 10,
                CampaignId = 20
            };
            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            // Set up local Mock variables to inspect what got logged
            string capturedFeaturesJson = string.Empty;
            _loggerMock.Setup(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Extracted ML Features")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            )).Callback<LogLevel, EventId, object, Exception?, object>((level, id, state, ex, func) =>
            {
                // The state is a FormattedLogValues containing our parameters
                capturedFeaturesJson = state?.ToString() ?? string.Empty;
            });

            // Act
            var result = await _service.GetMlPredictionAsync(donation, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Mock_v1-rules", result.ModelVersion); // Fell back to rules
            Assert.True(result.RiskScore >= 0);

            // Verify features logged
            Assert.Contains("1500", capturedFeaturesJson); // TransactionAmt matches amount
            Assert.Contains("testuser@gmail.com", user.Email ?? ""); // Verify domain extraction works

            // Parse features JSON from logger to verify elements
            var logPrefix = "Extracted ML Features for Donation 100: ";
            var startIndex = capturedFeaturesJson.IndexOf(logPrefix);
            if (startIndex >= 0)
            {
                var jsonPart = capturedFeaturesJson.Substring(startIndex + logPrefix.Length);
                var features = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonPart) ?? new Dictionary<string, object>();

                Assert.NotNull(features);
                Assert.True(features.ContainsKey("TransactionAmt"));
                Assert.Equal(1500.0, ((JsonElement)features["TransactionAmt"]).GetDouble());
                
                Assert.True(features.ContainsKey("D1"));
                Assert.True(((JsonElement)features["D1"]).GetDouble() >= 4.9); // Account age around 5 days
                
                Assert.True(features.ContainsKey("P_emaildomain"));
                Assert.Equal(12.0, ((JsonElement)features["P_emaildomain"]).GetDouble()); // gmail.com -> 12.0
                
                Assert.True(features.ContainsKey("dist2"));
                Assert.Equal(-999.0, ((JsonElement)features["dist2"]).GetDouble()); // Missing value -> -999.0
            }
        }
    }
}
