using System;

namespace DonationFraud.API.DTOs
{
    public class RegisterRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }

    public class LoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CreateCampaignDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
    }

    public class CreateDonationDto
    {
        public int CampaignId { get; set; }
        public decimal Amount { get; set; }
        
        public string IpAddress { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

    public class ReviewFraudAlertDto
    {
        public bool IsApproved { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}

namespace DonationFraud.API.Models
{
    public class ProcessDonationResult
    {
        public bool IsSuccess { get; set; }
        public int? DonationId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
