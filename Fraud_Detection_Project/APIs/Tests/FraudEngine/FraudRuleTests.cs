using DonationFraud.API.Entities;
using DonationFraud.API.Enums;
using DonationFraud.API.FraudEngine;
using Xunit;

namespace DonationFraud.Tests.FraudEngine
{
    public class HighFrequencyRuleTests
    {
        private readonly HighFrequencyRule _rule = new();

        [Fact]
        public async Task Evaluate_WhenDonationsExceedThreshold_ReturnsRiskScore()
        {
            // Arrange
            var config = new FraudRuleConfig
            {
                RuleName = "HighFrequency",
                Threshold = 3,
                RiskScoreContribution = 40,
                IsActive = true
            };

            var context = new DonationContext
            {
                CurrentDonation = new Donation { Id = 10, Amount = 100, IpAddress = "1.2.3.4" },
                UserRecentDonations = new List<Donation>
                {
                    new() { Id = 1, Amount = 50, IpAddress = "1.2.3.4" },
                    new() { Id = 2, Amount = 60, IpAddress = "1.2.3.4" },
                    new() { Id = 3, Amount = 70, IpAddress = "1.2.3.4" },
                    new() { Id = 4, Amount = 80, IpAddress = "1.2.3.4" }
                }
            };

            // Act
            var result = await _rule.EvaluateAsync(context, config);

            // Assert
            Assert.Equal(40, result.RiskScoreContribution);
            Assert.Contains("High frequency", result.Reason);
        }

        [Fact]
        public async Task Evaluate_WhenDonationsBelowThreshold_ReturnsZero()
        {
            // Arrange
            var config = new FraudRuleConfig
            {
                RuleName = "HighFrequency",
                Threshold = 5,
                RiskScoreContribution = 40,
                IsActive = true
            };

            var context = new DonationContext
            {
                CurrentDonation = new Donation { Id = 10, Amount = 100, IpAddress = "1.2.3.4" },
                UserRecentDonations = new List<Donation>
                {
                    new() { Id = 1, Amount = 50, IpAddress = "1.2.3.4" },
                    new() { Id = 2, Amount = 60, IpAddress = "1.2.3.4" }
                }
            };

            // Act
            var result = await _rule.EvaluateAsync(context, config);

            // Assert
            Assert.Equal(0, result.RiskScoreContribution);
            Assert.Empty(result.Reason);
        }
    }

    public class SameIPRuleTests
    {
        private readonly SameIPRule _rule = new();

        [Fact]
        public async Task Evaluate_WhenIPCountExceedsThreshold_ReturnsRiskScore()
        {
            // Arrange
            var config = new FraudRuleConfig
            {
                RuleName = "SameIP",
                Threshold = 2,
                RiskScoreContribution = 20,
                IsActive = true
            };

            var context = new DonationContext
            {
                CurrentDonation = new Donation { Id = 10, Amount = 100, IpAddress = "192.168.1.1" },
                UserRecentDonations = new List<Donation>
                {
                    new() { Id = 1, Amount = 50, IpAddress = "192.168.1.1" },
                    new() { Id = 2, Amount = 60, IpAddress = "192.168.1.1" },
                    new() { Id = 3, Amount = 70, IpAddress = "192.168.1.1" }
                }
            };

            // Act
            var result = await _rule.EvaluateAsync(context, config);

            // Assert
            Assert.Equal(20, result.RiskScoreContribution);
            Assert.Contains("same IP", result.Reason);
        }

        [Fact]
        public async Task Evaluate_WhenIPCountBelowThreshold_ReturnsZero()
        {
            // Arrange
            var config = new FraudRuleConfig
            {
                RuleName = "SameIP",
                Threshold = 5,
                RiskScoreContribution = 20,
                IsActive = true
            };

            var context = new DonationContext
            {
                CurrentDonation = new Donation { Id = 10, Amount = 100, IpAddress = "192.168.1.1" },
                UserRecentDonations = new List<Donation>
                {
                    new() { Id = 1, Amount = 50, IpAddress = "10.0.0.1" },
                    new() { Id = 2, Amount = 60, IpAddress = "192.168.1.1" }
                }
            };

            // Act
            var result = await _rule.EvaluateAsync(context, config);

            // Assert
            Assert.Equal(0, result.RiskScoreContribution);
            Assert.Empty(result.Reason);
        }

        [Fact]
        public async Task Evaluate_WhenDifferentIPs_ReturnsZero()
        {
            // Arrange
            var config = new FraudRuleConfig
            {
                RuleName = "SameIP",
                Threshold = 2,
                RiskScoreContribution = 20,
                IsActive = true
            };

            var context = new DonationContext
            {
                CurrentDonation = new Donation { Id = 10, Amount = 100, IpAddress = "192.168.1.1" },
                UserRecentDonations = new List<Donation>
                {
                    new() { Id = 1, Amount = 50, IpAddress = "10.0.0.1" },
                    new() { Id = 2, Amount = 60, IpAddress = "10.0.0.2" }
                }
            };

            // Act
            var result = await _rule.EvaluateAsync(context, config);

            // Assert
            Assert.Equal(0, result.RiskScoreContribution);
        }
    }

    public class SpikeRuleTests
    {
        private readonly SpikeRule _rule = new();

        [Fact]
        public async Task Evaluate_WhenAmountExceedsThreshold_ReturnsRiskScore()
        {
            // Arrange
            var config = new FraudRuleConfig
            {
                RuleName = "SpikeAmount",
                Threshold = 10000,
                RiskScoreContribution = 70,
                IsActive = true
            };

            var context = new DonationContext
            {
                CurrentDonation = new Donation { Id = 10, Amount = 50000, IpAddress = "1.2.3.4" },
                UserRecentDonations = new List<Donation>()
            };

            // Act
            var result = await _rule.EvaluateAsync(context, config);

            // Assert
            Assert.Equal(70, result.RiskScoreContribution);
            Assert.Contains("50000", result.Reason);
        }

        [Fact]
        public async Task Evaluate_WhenAmountBelowThreshold_ReturnsZero()
        {
            // Arrange
            var config = new FraudRuleConfig
            {
                RuleName = "SpikeAmount",
                Threshold = 10000,
                RiskScoreContribution = 70,
                IsActive = true
            };

            var context = new DonationContext
            {
                CurrentDonation = new Donation { Id = 10, Amount = 500, IpAddress = "1.2.3.4" },
                UserRecentDonations = new List<Donation>()
            };

            // Act
            var result = await _rule.EvaluateAsync(context, config);

            // Assert
            Assert.Equal(0, result.RiskScoreContribution);
            Assert.Empty(result.Reason);
        }

        [Fact]
        public async Task Evaluate_WhenAmountEqualsThreshold_ReturnsZero()
        {
            // Arrange: threshold is 10000, amount is exactly 10000 — should NOT flag (> not >=)
            var config = new FraudRuleConfig
            {
                RuleName = "SpikeAmount",
                Threshold = 10000,
                RiskScoreContribution = 70,
                IsActive = true
            };

            var context = new DonationContext
            {
                CurrentDonation = new Donation { Id = 10, Amount = 10000, IpAddress = "1.2.3.4" },
                UserRecentDonations = new List<Donation>()
            };

            // Act
            var result = await _rule.EvaluateAsync(context, config);

            // Assert
            Assert.Equal(0, result.RiskScoreContribution);
        }
    }
}
