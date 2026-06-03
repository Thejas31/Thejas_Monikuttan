using DonationFraud.API.Entities;
using DonationFraud.API.FraudEngine;
using DonationFraud.API.Interfaces;
using DonationFraud.API.Data;
using DonationFraud.API.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DonationFraud.API.Services
{
    public class FraudDetectionService : IFraudDetectionService
    {
        private readonly IFraudEvaluator _fraudEvaluator;
        private readonly IFraudFlagRepository _fraudFlagRepo;
        private readonly IMlInferenceService _mlInferenceService;
        private readonly DonationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;
        private readonly ILogger<FraudDetectionService> _logger;

        public FraudDetectionService(
            IFraudEvaluator fraudEvaluator, 
            IFraudFlagRepository fraudFlagRepo, 
            IMlInferenceService mlInferenceService,
            DonationDbContext context,
            IConfiguration configuration,
            IAuditService auditService, 
            ILogger<FraudDetectionService> logger)
        {
            _fraudEvaluator = fraudEvaluator;
            _fraudFlagRepo = fraudFlagRepo;
            _mlInferenceService = mlInferenceService;
            _context = context;
            _configuration = configuration;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> EvaluateAndFlagDonationAsync(Donation donation, int userId)
        {
            // 1. Run deterministic Layer 1 Rules
            var ruleResult = await _fraudEvaluator.EvaluateDonationAsync(donation, userId);

            // 2. Run predictive Layer 2 AI Model
            var mlPrediction = await _mlInferenceService.GetMlPredictionAsync(donation, userId);

            // 3. Log ML prediction to database audit trail
            var predictionRecord = new MlPrediction
            {
                DonationId = donation.Id,
                ModelVersion = mlPrediction.ModelVersion,
                PredictionProbability = (double)mlPrediction.RiskScore / 100.0,
                TopFeaturesImpact = mlPrediction.TopFeaturesImpact,
                EvaluatedAt = DateTime.UtcNow
            };
            _context.MlPredictions.Add(predictionRecord);
            await _context.SaveChangesAsync();

            // 4. Calculate blended risk score
            var wRules = double.TryParse(_configuration["FraudEngine:RulesWeight"], out var rW) ? rW : 0.4;
            var wAi = double.TryParse(_configuration["FraudEngine:AiWeight"], out var aW) ? aW : 0.6;

            int blendedScore = (int)Math.Round((wRules * ruleResult.TotalRiskScore) + (wAi * mlPrediction.RiskScore));
            blendedScore = Math.Min(100, Math.Max(0, blendedScore));

            var riskLevel = blendedScore >= 70 ? RiskLevel.High 
                          : (blendedScore >= 30 ? RiskLevel.Medium : RiskLevel.Low);

            // 5. Flag transaction if combined risk exceeds threshold
            if (blendedScore >= 30)
            {
                var reasons = new List<string>();
                if (ruleResult.TotalRiskScore > 0)
                {
                    reasons.Add($"Rules: {ruleResult.CombinedReasons}");
                }
                if (mlPrediction.RiskScore >= 30)
                {
                    reasons.Add($"AI: Flagged as {mlPrediction.RiskLevel} risk (Score: {mlPrediction.RiskScore})");
                }

                var flag = new FraudFlag
                {
                    DonationId = donation.Id,
                    RiskScore = blendedScore,
                    RuleRiskScore = ruleResult.TotalRiskScore,
                    AiRiskScore = mlPrediction.RiskScore,
                    RiskLevel = riskLevel,
                    Reason = string.Join(" | ", reasons),
                    CreatedAt = DateTime.UtcNow
                };

                await _fraudFlagRepo.AddFraudFlagAsync(flag);
                await _fraudFlagRepo.SaveChangesAsync();

                _logger.LogWarning("Fraud detected for Donation {DonationId}. Blended Score: {BlendedScore} (Rules: {RuleRisk}, AI: {AiRisk})", 
                    donation.Id, blendedScore, ruleResult.TotalRiskScore, mlPrediction.RiskScore);
                
                await _auditService.LogActionAsync($"Fraud triggered for Donation {donation.Id}. Blended: {blendedScore}", userId, "FraudFlag");

                if (riskLevel == RiskLevel.High)
                {
                    return true; // Blocks the transaction
                }
            }

            return false;
        }
    }
}
