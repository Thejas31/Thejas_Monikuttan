using DonationFraud.API.Entities;
using DonationFraud.API.FraudEngine;
using DonationFraud.API.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DonationFraud.API.Services
{
    public class FraudDetectionService : IFraudDetectionService
    {
        private readonly IFraudEvaluator _fraudEvaluator;
        private readonly IFraudFlagRepository _fraudFlagRepo;
        private readonly IAuditService _auditService;
        private readonly ILogger<FraudDetectionService> _logger;

        public FraudDetectionService(IFraudEvaluator fraudEvaluator, IFraudFlagRepository fraudFlagRepo, IAuditService auditService, ILogger<FraudDetectionService> logger)
        {
            _fraudEvaluator = fraudEvaluator;
            _fraudFlagRepo = fraudFlagRepo;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> EvaluateAndFlagDonationAsync(Donation donation, int userId)
        {
            var fraudResult = await _fraudEvaluator.EvaluateDonationAsync(donation, userId);

            if (fraudResult.TotalRiskScore >= 30) // Threshold for flagging
            {
                var flag = new FraudFlag
                {
                    DonationId = donation.Id,
                    RiskScore = fraudResult.TotalRiskScore,
                    RiskLevel = fraudResult.RiskLevel,
                    Reason = fraudResult.CombinedReasons
                };
                
                await _fraudFlagRepo.AddFraudFlagAsync(flag);
                await _fraudFlagRepo.SaveChangesAsync();

                _logger.LogWarning("Fraud detected for Donation {DonationId}. Risk Score: {RiskScore}, Reason: {Reason}", donation.Id, fraudResult.TotalRiskScore, fraudResult.CombinedReasons);
                await _auditService.LogActionAsync($"Fraud triggered for Donation {donation.Id}. Score: {fraudResult.TotalRiskScore}", userId, "FraudFlag");

                if (fraudResult.RiskLevel == Enums.RiskLevel.High)
                {
                    return true; // Indicates it was flagged as HIGH risk and should be blocked
                }
            }

            return false; // Indicates it passed or was only medium risk
        }
    }
}
