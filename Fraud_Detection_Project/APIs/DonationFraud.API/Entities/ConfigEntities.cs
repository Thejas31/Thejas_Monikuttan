using System;

namespace DonationFraud.API.Entities
{
    public class FraudRuleConfig
    {
        public int Id { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public decimal Threshold { get; set; }
        public int RiskScoreContribution { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
