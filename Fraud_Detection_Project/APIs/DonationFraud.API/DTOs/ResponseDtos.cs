using System;
using System.Collections.Generic;

namespace DonationFraud.API.DTOs
{
    // ====== Campaign Response DTOs ======

    public class CampaignResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalDonations { get; set; }
        public decimal TotalAmountRaised { get; set; }
        public bool IsActive { get; set; }
        public List<CampaignDonationDto> Donations { get; set; } = new();
    }

    public class CampaignDonationDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string DonorName { get; set; } = string.Empty;
        public string DonorEmail { get; set; } = string.Empty;
        public bool? IsApproved { get; set; }
    }

    // ====== Donation Response DTOs ======

    public class DonationResponseDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public int CampaignId { get; set; }
        public string CampaignTitle { get; set; } = string.Empty;
        public bool IsFlagged { get; set; }
        public string? FraudReason { get; set; }
        public string? RiskLevel { get; set; }
        public bool? IsApproved { get; set; }
        public string? AdminNotes { get; set; }
    }

    // ====== Fraud Alert Response DTOs ======

    public class FraudAlertResponseDto
    {
        public int Id { get; set; }
        public int DonationId { get; set; }
        public decimal DonationAmount { get; set; }
        public DateTime DonationTimestamp { get; set; }
        public int RiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public bool? IsApproved { get; set; }
        public string? AdminNotes { get; set; }
        public DateTime CreatedAt { get; set; }

        // Donor info (flattened)
        public int DonorUserId { get; set; }
        public string DonorUsername { get; set; } = string.Empty;

        // Visual metadata for Fraud Alerts Tracking
        public string IpAddress { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }

    // ====== User Summary DTO (for embedding in other responses) ======

    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
