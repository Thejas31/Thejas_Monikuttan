using DonationFraud.API.Entities;
using System.Threading.Tasks;

namespace DonationFraud.API.Interfaces
{
    public interface IFraudDetectionService
    {
        Task<bool> EvaluateAndFlagDonationAsync(Donation donation, int userId);
    }
}
