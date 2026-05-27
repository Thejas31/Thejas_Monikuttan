using DonationFraud.API.Data;
using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using DonationFraud.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DonationFraud.API.Services
{
    public class DonationService : IDonationService
    {
        private readonly IDonationRepository _donationRepo;
        private readonly ICampaignRepository _campaignRepo;
        private readonly IFraudDetectionService _fraudDetectionService;
        private readonly IAuditService _auditService;
        private readonly DonationDbContext _context;
        private readonly ILogger<DonationService> _logger;

        public DonationService(
            IDonationRepository donationRepo, 
            ICampaignRepository campaignRepo,
            IFraudDetectionService fraudDetectionService, 
            IAuditService auditService, 
            DonationDbContext context,
            ILogger<DonationService> logger)
        {
            _donationRepo = donationRepo;
            _campaignRepo = campaignRepo;
            _fraudDetectionService = fraudDetectionService;
            _auditService = auditService;
            _context = context;
            _logger = logger;
        }

        public async Task<ProcessDonationResult> ProcessDonationAsync(CreateDonationDto request)
        {
            var campaignInfo = await _context.Campaigns
                .Where(c => c.Id == request.CampaignId)
                .Select(c => new { c.IsActive, c.TargetAmount })
                .FirstOrDefaultAsync();

            if (campaignInfo == null)
            {
                return new ProcessDonationResult { IsSuccess = false, Reason = "Campaign not found." };
            }
            if (!campaignInfo.IsActive)
            {
                return new ProcessDonationResult { IsSuccess = false, Reason = "This campaign is inactive." };
            }

            var donation = new Donation
            {
                CampaignId = request.CampaignId,
                UserId = request.UserId,
                Amount = request.Amount,
                IpAddress = request.IpAddress
            };

            // Save donation first to get ID
            await _donationRepo.AddDonationAsync(donation);
            await _donationRepo.SaveChangesAsync();

            _logger.LogInformation("Donation {DonationId} created by user {UserId} for amount {Amount}", donation.Id, request.UserId, request.Amount);
            await _auditService.LogActionAsync($"Donation created for amount {request.Amount}", request.UserId, "Donation");

            // Delegate fraud check to the FraudDetectionService
            bool isHighRisk = await _fraudDetectionService.EvaluateAndFlagDonationAsync(donation, request.UserId);

            if (isHighRisk)
            {
                return new ProcessDonationResult { IsSuccess = false, Reason = "Transaction blocked due to high fraud risk." };
            }

            // Recalculate total raised and auto-deactivate if target is met
            var campaignDetails = await _context.Campaigns
                .Where(c => c.Id == request.CampaignId)
                .Select(c => new
                {
                    c.TargetAmount,
                    TotalRaised = c.Donations
                        .Where(d => d.FraudFlag == null || 
                                    d.FraudFlag.IsApproved == true || 
                                    (d.FraudFlag.IsApproved == null && d.FraudFlag.RiskLevel != Enums.RiskLevel.High))
                        .Sum(d => d.Amount)
                })
                .FirstOrDefaultAsync();

            if (campaignDetails != null && campaignDetails.TotalRaised >= campaignDetails.TargetAmount)
            {
                var campaignToDeactivate = await _context.Campaigns.FindAsync(request.CampaignId);
                if (campaignToDeactivate != null)
                {
                    campaignToDeactivate.IsActive = false;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Campaign {CampaignId} has automatically reached its target of ₹{TargetAmount} (raised ₹{TotalRaised}) and has been closed.", campaignToDeactivate.Id, campaignToDeactivate.TargetAmount, campaignDetails.TotalRaised);
                    await _auditService.LogActionAsync($"Campaign {campaignToDeactivate.Id} automatically deactivated (Target Reached)", request.UserId, "Campaign");
                }
            }

            return new ProcessDonationResult { IsSuccess = true, DonationId = donation.Id };
        }

        public async Task<IEnumerable<DonationResponseDto>> GetUserDonationsAsync(int userId)
        {
            var donations = await _donationRepo.GetDonationsByUserIdAsync(userId);
            return donations.Select(d => new DonationResponseDto
            {
                Id = d.Id,
                Amount = d.Amount,
                Timestamp = d.Timestamp,
                CampaignId = d.CampaignId,
                CampaignTitle = d.Campaign?.Title ?? string.Empty,
                IsFlagged = d.FraudFlag != null,
                FraudReason = d.FraudFlag?.Reason,
                RiskLevel = d.FraudFlag?.RiskLevel.ToString(),
                IsApproved = d.FraudFlag != null ? d.FraudFlag.IsApproved : true,
                AdminNotes = d.FraudFlag?.AdminNotes
            });
        }
    }
}
