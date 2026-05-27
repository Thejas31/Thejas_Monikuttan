using DonationFraud.API.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DonationFraud.API.Interfaces
{
    public interface IFraudManagementService
    {
        Task<IEnumerable<FraudAlertResponseDto>> GetAllAlertsAsync();
        Task<IEnumerable<FraudAlertResponseDto>> GetHighRiskAlertsAsync();
        Task<bool> ReviewAlertAsync(int flagId, bool isApproved, string notes, int adminUserId);
        Task<DashboardStatsDto> GetDashboardStatsAsync();
    }
}
