using DonationFraud.API.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DonationFraud.API.Interfaces
{
    public interface ICampaignService
    {
        Task<int> CreateCampaignAsync(CreateCampaignDto request);
        Task<IEnumerable<CampaignResponseDto>> GetAllCampaignsAsync();
        Task<CampaignResponseDto?> GetCampaignByIdAsync(int id);
        Task<bool> EndCampaignAsync(int id);
    }
}
