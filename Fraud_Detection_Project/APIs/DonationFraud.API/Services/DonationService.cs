using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using DonationFraud.API.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DonationFraud.API.Services
{
    public class DonationService : IDonationService
    {
        private readonly IDonationRepository _donationRepo;
        private readonly IFraudDetectionService _fraudDetectionService;
        private readonly IAuditService _auditService;
        private readonly ILogger<DonationService> _logger;

        public DonationService(IDonationRepository donationRepo, IFraudDetectionService fraudDetectionService, IAuditService auditService, ILogger<DonationService> logger)
        {
            _donationRepo = donationRepo;
            _fraudDetectionService = fraudDetectionService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ProcessDonationResult> ProcessDonationAsync(CreateDonationDto request)
        {
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

            return new ProcessDonationResult { IsSuccess = true, DonationId = donation.Id };
        }

        public async Task<IEnumerable<Donation>> GetUserDonationsAsync(int userId) => await _donationRepo.GetDonationsByUserIdAsync(userId);
    }
}
