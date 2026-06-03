using DonationFraud.API.Entities;
using DonationFraud.API.Models;
using System.Threading.Tasks;

namespace DonationFraud.API.Interfaces
{
    public interface IMlInferenceService
    {
        Task<MlPredictionResult> GetMlPredictionAsync(Donation donation, int userId);
    }
}
