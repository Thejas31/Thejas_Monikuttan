using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DonationFraud.API.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepo;
        public CampaignService(ICampaignRepository campaignRepo) => _campaignRepo = campaignRepo;

        public async Task<int> CreateCampaignAsync(CreateCampaignDto request)
        {
            var campaign = new Campaign
            {
                Title = request.Title,
                Description = request.Description,
                TargetAmount = request.TargetAmount
            };
            await _campaignRepo.AddCampaignAsync(campaign);
            await _campaignRepo.SaveChangesAsync();
            return campaign.Id;
        }

        public async Task<IEnumerable<Campaign>> GetAllCampaignsAsync() => await _campaignRepo.GetAllCampaignsAsync();
        public async Task<Campaign?> GetCampaignByIdAsync(int id) => await _campaignRepo.GetCampaignByIdAsync(id);
    }
}
