using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DonationFraud.API.Interfaces
{
    public interface ICampaignService
    {
        Task<int> CreateCampaignAsync(CreateCampaignDto request);
        Task<IEnumerable<Campaign>> GetAllCampaignsAsync();
        Task<Campaign?> GetCampaignByIdAsync(int id);
    }
}
