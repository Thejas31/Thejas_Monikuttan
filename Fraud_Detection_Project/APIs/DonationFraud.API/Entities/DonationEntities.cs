using DonationFraud.API.Enums;
using System;

namespace DonationFraud.API.Entities
{
    public class Donation
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int CampaignId { get; set; }
        public Campaign Campaign { get; set; } = null!;

        public FraudFlag? FraudFlag { get; set; }
    }

    public class FraudFlag
    {
        public int Id { get; set; }
        public int DonationId { get; set; }
        public Donation Donation { get; set; } = null!;

        public int RiskScore { get; set; } // 0-100
        public RiskLevel RiskLevel { get; set; } 
        public string Reason { get; set; } = string.Empty;

        // Management
        public bool? IsApproved { get; set; } // null = pending, true = approved, false = blocked
        public string? AdminNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
