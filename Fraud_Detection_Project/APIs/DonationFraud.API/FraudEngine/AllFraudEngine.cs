using DonationFraud.API.Data;
using DonationFraud.API.Entities;
using DonationFraud.API.Enums;
using DonationFraud.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DonationFraud.API.FraudEngine
{
    public interface IFraudRule
    {
        Task<RuleEvaluationResult> EvaluateAsync(DonationContext context, FraudRuleConfig config);
        string RuleName { get; }
    }

    public class DonationContext
    {
        public Donation CurrentDonation { get; set; } = null!;
        public IEnumerable<Donation> UserRecentDonations { get; set; } = new List<Donation>();
    }

    public class RuleEvaluationResult
    {
        public int RiskScoreContribution { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class HighFrequencyRule : IFraudRule
    {
        public string RuleName => "HighFrequency";

        public Task<RuleEvaluationResult> EvaluateAsync(DonationContext context, FraudRuleConfig config)
        {
            var result = new RuleEvaluationResult();
            
            if (context.UserRecentDonations.Count() > config.Threshold)
            {
                result.RiskScoreContribution = config.RiskScoreContribution;
                result.Reason = $"High frequency of donations detected (>{config.Threshold}).";
            }

            return Task.FromResult(result);
        }
    }

    public class SameIPRule : IFraudRule
    {
        public string RuleName => "SameIP";

        public Task<RuleEvaluationResult> EvaluateAsync(DonationContext context, FraudRuleConfig config)
        {
            var result = new RuleEvaluationResult();
            
            var recentFromIp = context.UserRecentDonations.Count(d => d.IpAddress == context.CurrentDonation.IpAddress);
            if (recentFromIp > config.Threshold)
            {
                result.RiskScoreContribution = config.RiskScoreContribution;
                result.Reason = $"Unusual volume from the same IP address (>{config.Threshold}).";
            }

            return Task.FromResult(result);
        }
    }

    public class SpikeRule : IFraudRule
    {
        public string RuleName => "SpikeAmount";

        public Task<RuleEvaluationResult> EvaluateAsync(DonationContext context, FraudRuleConfig config)
        {
            var result = new RuleEvaluationResult();
            
            if (context.CurrentDonation.Amount > config.Threshold)
            {
                result.RiskScoreContribution = config.RiskScoreContribution;
                result.Reason = $"Unusually high amount spiked: {context.CurrentDonation.Amount} (Threshold: {config.Threshold})";
            }

            return Task.FromResult(result);
        }
    }

    public interface IFraudEvaluator
    {
        Task<FraudResult> EvaluateDonationAsync(Donation donation, int userId);
    }

    public class FraudResult
    {
        public int TotalRiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public string CombinedReasons { get; set; } = string.Empty;
    }

    public class FraudEvaluator : IFraudEvaluator
    {
        private readonly IEnumerable<IFraudRule> _rules;
        private readonly IDonationRepository _donationRepo;
        private readonly DonationDbContext _dbContext;

        public FraudEvaluator(IEnumerable<IFraudRule> rules, IDonationRepository donationRepo, DonationDbContext dbContext)
        {
            _rules = rules;
            _donationRepo = donationRepo;
            _dbContext = dbContext;
        }

        public async Task<FraudResult> EvaluateDonationAsync(Donation donation, int userId)
        {
            var recentDonations = await _donationRepo.GetUserDonationsInTimespanAsync(userId, TimeSpan.FromMinutes(15));
            var context = new DonationContext
            {
                CurrentDonation = donation,
                UserRecentDonations = recentDonations
            };

            var activeConfigs = await _dbContext.FraudRuleConfigs.Where(r => r.IsActive).ToListAsync();

            int totalScore = 0;
            var reasons = new List<string>();

            foreach (var config in activeConfigs)
            {
                var rule = _rules.FirstOrDefault(r => r.RuleName == config.RuleName);
                if (rule != null)
                {
                    var ruleResult = await rule.EvaluateAsync(context, config);
                    if (ruleResult.RiskScoreContribution > 0)
                    {
                        totalScore += ruleResult.RiskScoreContribution;
                        reasons.Add(ruleResult.Reason);
                    }
                }
            }

            var result = new FraudResult
            {
                TotalRiskScore = Math.Min(100, totalScore),
                CombinedReasons = string.Join(" | ", reasons)
            };

            if (result.TotalRiskScore >= 70) result.RiskLevel = RiskLevel.High;
            else if (result.TotalRiskScore >= 30) result.RiskLevel = RiskLevel.Medium;
            else result.RiskLevel = RiskLevel.Low;

            return result;
        }
    }
}
