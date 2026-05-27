using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using DonationFraud.API.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DonationFraud.API.Services
{
    public class FraudManagementService : IFraudManagementService
    {
        private readonly IFraudFlagRepository _fraudRepo;
        private readonly IAuditService _auditService;
        private readonly DonationDbContext _context;

        public FraudManagementService(IFraudFlagRepository fraudRepo, IAuditService auditService, DonationDbContext context)
        {
            _fraudRepo = fraudRepo;
            _auditService = auditService;
            _context = context;
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

            // If a flagged donation gets approved, check if the campaign has hit its target
            if (isApproved)
            {
                var donation = flag.Donation ?? await _context.Donations.Include(d => d.Campaign).FirstOrDefaultAsync(d => d.Id == flag.DonationId);
                if (donation != null && donation.Campaign != null)
                {
                    // Reload campaign with all its donations to check current sum
                    var campaign = await _context.Campaigns.Include(c => c.Donations).ThenInclude(d => d.FraudFlag).FirstOrDefaultAsync(c => c.Id == donation.CampaignId);
                    if (campaign != null)
                    {
                        var totalRaised = campaign.Donations?
                            .Where(d => d.FraudFlag == null || 
                                        d.FraudFlag.IsApproved == true || 
                                        (d.FraudFlag.IsApproved == null && d.FraudFlag.RiskLevel != Enums.RiskLevel.High))
                            .Sum(d => d.Amount) ?? 0;

                        if (totalRaised >= campaign.TargetAmount)
                        {
                            campaign.IsActive = false;
                            await _context.SaveChangesAsync();
                            await _auditService.LogActionAsync($"Campaign {campaign.Id} automatically deactivated after approved donation (Target Reached)", adminUserId, "Campaign");
                        }
                    }
                }
            }

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

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var totalAmount = await _context.Donations
                .Include(d => d.FraudFlag)
                .Where(d => d.FraudFlag == null || d.FraudFlag.IsApproved == true)
                .SumAsync(d => d.Amount);

            string totalDonationsStr;
            if (totalAmount >= 10000000)
                totalDonationsStr = $"₹{(totalAmount / 10000000m):F1}Cr";
            else if (totalAmount >= 100000)
                totalDonationsStr = $"₹{(totalAmount / 100000m):F1}L";
            else if (totalAmount >= 1000)
                totalDonationsStr = $"₹{(totalAmount / 1000m):F1}K";
            else
                totalDonationsStr = $"₹{totalAmount:N0}";

            var todayUtc = DateTime.UtcNow.Date;
            var transactionsToday = await _context.Donations
                .CountAsync(d => d.Timestamp >= todayUtc);

            var flaggedDonations = await _context.FraudFlags.CountAsync();

            var confirmedFraud = await _context.FraudFlags
                .CountAsync(f => f.IsApproved == false);

            return new DashboardStatsDto
            {
                TotalDonations = totalDonationsStr,
                TransactionsToday = transactionsToday,
                FlaggedDonations = flaggedDonations,
                ConfirmedFraud = confirmedFraud
            };
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
