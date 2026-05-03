using DonationFraud.API.Data;
using DonationFraud.API.Entities;
using DonationFraud.API.Enums;
using DonationFraud.API.FraudEngine;
using DonationFraud.API.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DonationFraud.Tests.FraudEngine
{
    public class FraudEvaluatorTests : IDisposable
    {
        private readonly DonationDbContext _dbContext;

        public FraudEvaluatorTests()
        {
            var options = new DbContextOptionsBuilder<DonationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new DonationDbContext(options);
        }

        public void Dispose() => _dbContext.Dispose();

        private async Task SeedDefaults()
        {
            // Seed fraud rule configs
            _dbContext.FraudRuleConfigs.AddRange(
                new FraudRuleConfig { RuleName = "HighFrequency", Threshold = 3, RiskScoreContribution = 40, IsActive = true },
                new FraudRuleConfig { RuleName = "SameIP", Threshold = 2, RiskScoreContribution = 20, IsActive = true },
                new FraudRuleConfig { RuleName = "SpikeAmount", Threshold = 10000, RiskScoreContribution = 70, IsActive = true }
            );

            // Seed a user and campaign for FK constraints
            _dbContext.Roles.Add(new Role { Id = 1, Name = "User" });
            _dbContext.Users.Add(new User { Id = 1, Username = "testuser", Email = "test@test.com", PasswordHash = "hash", RoleId = 1 });
            _dbContext.Campaigns.Add(new Campaign { Id = 1, Title = "Test Campaign", Description = "Test", TargetAmount = 5000 });

            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task EvaluateDonation_NoViolations_ReturnsLowRisk()
        {
            // Arrange
            await SeedDefaults();
            var donationRepo = new DonationRepository(_dbContext);
            var rules = new IFraudRule[] { new HighFrequencyRule(), new SameIPRule(), new SpikeRule() };
            var evaluator = new FraudEvaluator(rules, donationRepo, _dbContext);

            var donation = new Donation { Id = 100, Amount = 50, IpAddress = "1.2.3.4", UserId = 1, CampaignId = 1 };

            // Act
            var result = await evaluator.EvaluateDonationAsync(donation, 1);

            // Assert
            Assert.Equal(RiskLevel.Low, result.RiskLevel);
            Assert.Equal(0, result.TotalRiskScore);
        }

        [Fact]
        public async Task EvaluateDonation_SpikeTriggered_ReturnsMediumOrHighRisk()
        {
            // Arrange
            await SeedDefaults();
            var donationRepo = new DonationRepository(_dbContext);
            var rules = new IFraudRule[] { new HighFrequencyRule(), new SameIPRule(), new SpikeRule() };
            var evaluator = new FraudEvaluator(rules, donationRepo, _dbContext);

            var donation = new Donation { Id = 101, Amount = 50000, IpAddress = "1.2.3.4", UserId = 1, CampaignId = 1 };

            // Act
            var result = await evaluator.EvaluateDonationAsync(donation, 1);

            // Assert
            Assert.True(result.TotalRiskScore >= 70); // SpikeAmount contributes 70
            Assert.Equal(RiskLevel.High, result.RiskLevel);
            Assert.Contains("spiked", result.CombinedReasons);
        }

        [Fact]
        public async Task EvaluateDonation_HighFrequencyTriggered_ReturnsMediumRisk()
        {
            // Arrange
            await SeedDefaults();

            // Pre-seed 4 recent donations for user 1
            for (int i = 1; i <= 4; i++)
            {
                _dbContext.Donations.Add(new Donation
                {
                    Amount = 50,
                    IpAddress = "10.0.0.1",
                    UserId = 1,
                    CampaignId = 1,
                    Timestamp = DateTime.UtcNow.AddMinutes(-5) // Recent
                });
            }
            await _dbContext.SaveChangesAsync();

            var donationRepo = new DonationRepository(_dbContext);
            var rules = new IFraudRule[] { new HighFrequencyRule(), new SameIPRule(), new SpikeRule() };
            var evaluator = new FraudEvaluator(rules, donationRepo, _dbContext);

            var donation = new Donation { Id = 200, Amount = 50, IpAddress = "10.0.0.1", UserId = 1, CampaignId = 1 };

            // Act
            var result = await evaluator.EvaluateDonationAsync(donation, 1);

            // Assert
            Assert.True(result.TotalRiskScore >= 30);
            Assert.True(result.RiskLevel == RiskLevel.Medium || result.RiskLevel == RiskLevel.High);
        }

        [Fact]
        public async Task EvaluateDonation_MultipleRulesTriggered_ScoresAggregate()
        {
            // Arrange
            await SeedDefaults();

            // Pre-seed 4 recent donations from the same IP
            for (int i = 1; i <= 4; i++)
            {
                _dbContext.Donations.Add(new Donation
                {
                    Amount = 50,
                    IpAddress = "192.168.1.1",
                    UserId = 1,
                    CampaignId = 1,
                    Timestamp = DateTime.UtcNow.AddMinutes(-2)
                });
            }
            await _dbContext.SaveChangesAsync();

            var donationRepo = new DonationRepository(_dbContext);
            var rules = new IFraudRule[] { new HighFrequencyRule(), new SameIPRule(), new SpikeRule() };
            var evaluator = new FraudEvaluator(rules, donationRepo, _dbContext);

            // This donation also has high amount + same IP + high frequency
            var donation = new Donation { Id = 300, Amount = 50000, IpAddress = "192.168.1.1", UserId = 1, CampaignId = 1 };

            // Act
            var result = await evaluator.EvaluateDonationAsync(donation, 1);

            // Assert: Score capped at 100
            Assert.True(result.TotalRiskScore <= 100);
            Assert.Equal(RiskLevel.High, result.RiskLevel);
        }

        [Fact]
        public async Task EvaluateDonation_DisabledRulesNotEvaluated()
        {
            // Arrange — seed configs but disable SpikeAmount
            _dbContext.FraudRuleConfigs.AddRange(
                new FraudRuleConfig { RuleName = "HighFrequency", Threshold = 3, RiskScoreContribution = 40, IsActive = true },
                new FraudRuleConfig { RuleName = "SameIP", Threshold = 2, RiskScoreContribution = 20, IsActive = true },
                new FraudRuleConfig { RuleName = "SpikeAmount", Threshold = 10000, RiskScoreContribution = 70, IsActive = false }
            );
            _dbContext.Roles.Add(new Role { Id = 1, Name = "User" });
            _dbContext.Users.Add(new User { Id = 1, Username = "testuser", Email = "test@test.com", PasswordHash = "hash", RoleId = 1 });
            _dbContext.Campaigns.Add(new Campaign { Id = 1, Title = "Test Campaign", Description = "Test", TargetAmount = 5000 });
            await _dbContext.SaveChangesAsync();

            var donationRepo = new DonationRepository(_dbContext);
            var rules = new IFraudRule[] { new HighFrequencyRule(), new SameIPRule(), new SpikeRule() };
            var evaluator = new FraudEvaluator(rules, donationRepo, _dbContext);

            // This donation exceeds spike threshold, but the rule is disabled
            var donation = new Donation { Id = 400, Amount = 50000, IpAddress = "1.2.3.4", UserId = 1, CampaignId = 1 };

            // Act
            var result = await evaluator.EvaluateDonationAsync(donation, 1);

            // Assert: Spike rule is disabled, so score should be 0
            Assert.Equal(0, result.TotalRiskScore);
            Assert.Equal(RiskLevel.Low, result.RiskLevel);
        }
    }
}
