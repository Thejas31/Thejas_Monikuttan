using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<IEnumerable<CampaignResponseDto>> GetAllCampaignsAsync()
        {
            var campaigns = await _campaignRepo.GetAllCampaignsAsync();
            return campaigns.Select(MapToDto);
        }

        public async Task<CampaignResponseDto?> GetCampaignByIdAsync(int id)
        {
            var campaign = await _campaignRepo.GetCampaignByIdAsync(id);
            return campaign == null ? null : MapToDto(campaign);
        }

        public async Task<bool> EndCampaignAsync(int id)
        {
            var campaign = await _campaignRepo.GetCampaignByIdAsync(id);
            if (campaign == null) return false;

            campaign.IsActive = false;
            await _campaignRepo.SaveChangesAsync();
            return true;
        }

        private static CampaignResponseDto MapToDto(Campaign c) => new()
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            TargetAmount = c.TargetAmount,
            CreatedAt = c.CreatedAt,
            TotalDonations = c.Donations?.Count ?? 0,
            TotalAmountRaised = c.Donations?.Sum(d => d.Amount) ?? 0,
            IsActive = c.IsActive,
            Donations = c.Donations?.Select(d => new CampaignDonationDto
            {
                Id = d.Id,
                Amount = d.Amount,
                Timestamp = d.Timestamp,
                DonorName = d.User != null ? $"{d.User.FirstName} {d.User.LastName}".Trim() : "Anonymous",
                DonorEmail = d.User?.Email ?? string.Empty,
                IsApproved = d.FraudFlag != null ? d.FraudFlag.IsApproved : true
            }).ToList() ?? new List<CampaignDonationDto>()
        };
    }
}
