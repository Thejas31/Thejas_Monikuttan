using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using System.Collections.Generic;
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

        public async Task<IEnumerable<FraudFlag>> GetAllAlertsAsync() => await _fraudRepo.GetAllFlagsAsync();
        public async Task<IEnumerable<FraudFlag>> GetHighRiskAlertsAsync() => await _fraudRepo.GetHighRiskFlagsAsync();

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
    }
}
