using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DonationFraud.API.Services
{
    public class FraudManagementService : IFraudManagementService
    {
        private readonly IFraudFlagRepository _fraudRepo;
        private readonly IAuditService _auditService;

        public FraudManagementService(IFraudFlagRepository fraudRepo, IAuditService auditService)
        {
            _fraudRepo = fraudRepo;
            _auditService = auditService;
        }

        public async Task<IEnumerable<FraudAlertResponseDto>> GetAllAlertsAsync()
        {
            var flags = await _fraudRepo.GetAllFlagsAsync();
            return flags.Select(MapToDto);
        }

        public async Task<IEnumerable<FraudAlertResponseDto>> GetHighRiskAlertsAsync()
        {
            var flags = await _fraudRepo.GetHighRiskFlagsAsync();
            return flags.Select(MapToDto);
        }

        public async Task<bool> ReviewAlertAsync(int flagId, bool isApproved, string notes, int adminUserId)
        {
            var flag = await _fraudRepo.GetFraudFlagByIdAsync(flagId);
            if (flag == null) return false;

            flag.IsApproved = isApproved;
            flag.AdminNotes = notes;
            await _fraudRepo.SaveChangesAsync();

            await _auditService.LogActionAsync($"Reviewed FraudFlag {flagId} (Approved: {isApproved})", adminUserId, "FraudFlag");

            return true;
        }

        private static string GetCountryFromIp(string? ip)
        {
            if (string.IsNullOrEmpty(ip) || ip == "127.0.0.1" || ip == "::1" || ip == "localhost")
                return "United States";
            
            if (ip.StartsWith("192.168")) return "Canada";
            if (ip.StartsWith("10.")) return "United Kingdom";
            if (ip.StartsWith("172.")) return "Germany";
            
            int hash = System.Math.Abs(ip.GetHashCode());
            string[] countries = { "India", "Canada", "United Kingdom", "Germany", "Australia", "Singapore", "Japan", "France" };
            return countries[hash % countries.Length];
        }

        private static string GetPaymentMethodForDonation(int donationId)
        {
            string[] methods = { "Credit Card", "UPI", "Net Banking", "Debit Card", "PayPal" };
            return methods[donationId % methods.Length];
        }

        private static FraudAlertResponseDto MapToDto(FraudFlag f) => new()
        {
            Id = f.Id,
            DonationId = f.DonationId,
            DonationAmount = f.Donation?.Amount ?? 0,
            DonationTimestamp = f.Donation?.Timestamp ?? default,
            RiskScore = f.RiskScore,
            RiskLevel = f.RiskLevel.ToString(),
            Reason = f.Reason,
            IsApproved = f.IsApproved,
            AdminNotes = f.AdminNotes,
            CreatedAt = f.CreatedAt,
            DonorUserId = f.Donation?.UserId ?? 0,
            DonorUsername = f.Donation?.User?.Username ?? string.Empty,
            IpAddress = f.Donation?.IpAddress ?? "Unknown",
            Country = GetCountryFromIp(f.Donation?.IpAddress),
            PaymentMethod = GetPaymentMethodForDonation(f.DonationId)
        };
    }
}
