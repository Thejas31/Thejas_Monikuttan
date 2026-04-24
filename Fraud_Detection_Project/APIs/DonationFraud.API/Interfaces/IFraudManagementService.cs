using DonationFraud.API.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DonationFraud.API.Interfaces
{
    public interface IFraudManagementService
    {
        Task<IEnumerable<FraudFlag>> GetAllAlertsAsync();
        Task<IEnumerable<FraudFlag>> GetHighRiskAlertsAsync();
        Task<bool> ReviewAlertAsync(int flagId, bool isApproved, string notes, int adminUserId);
    }
}
