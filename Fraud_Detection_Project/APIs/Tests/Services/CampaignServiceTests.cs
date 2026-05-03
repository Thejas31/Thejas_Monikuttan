using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using DonationFraud.API.Services;
using Moq;
using Xunit;

namespace DonationFraud.Tests.Services
{
    public class CampaignServiceTests
    {
        private readonly Mock<ICampaignRepository> _repoMock;
        private readonly CampaignService _service;

        public CampaignServiceTests()
        {
            _repoMock = new Mock<ICampaignRepository>();
            _repoMock.Setup(r => r.AddCampaignAsync(It.IsAny<Campaign>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _service = new CampaignService(_repoMock.Object);
        }

        [Fact]
        public async Task CreateCampaign_ReturnsId()
        {
            var result = await _service.CreateCampaignAsync(new CreateCampaignDto { Title = "Save the Whales", Description = "Ocean campaign", TargetAmount = 10000 });
            _repoMock.Verify(r => r.AddCampaignAsync(It.Is<Campaign>(c => c.Title == "Save the Whales")), Times.Once);
        }

        [Fact]
        public async Task GetAllCampaigns_ReturnsDtos()
        {
            var campaigns = new List<Campaign>
            {
                new() { Id = 1, Title = "A", Description = "D", TargetAmount = 5000, Donations = new List<Donation> { new() { Amount = 100 }, new() { Amount = 200 } } },
                new() { Id = 2, Title = "B", Description = "D", TargetAmount = 10000, Donations = new List<Donation>() }
            };
            _repoMock.Setup(r => r.GetAllCampaignsAsync()).ReturnsAsync(campaigns);

            var result = (await _service.GetAllCampaignsAsync()).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(2, result[0].TotalDonations);
            Assert.Equal(300, result[0].TotalAmountRaised);
            Assert.Equal(0, result[1].TotalDonations);
        }

        [Fact]
        public async Task GetCampaignById_NotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetCampaignByIdAsync(999)).ReturnsAsync((Campaign?)null);
            var result = await _service.GetCampaignByIdAsync(999);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCampaignById_Found_ReturnsDto()
        {
            var campaign = new Campaign { Id = 1, Title = "Test", Description = "D", TargetAmount = 5000, Donations = new List<Donation> { new() { Amount = 500 } } };
            _repoMock.Setup(r => r.GetCampaignByIdAsync(1)).ReturnsAsync(campaign);

            var result = await _service.GetCampaignByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Test", result!.Title);
            Assert.Equal(1, result.TotalDonations);
            Assert.Equal(500, result.TotalAmountRaised);
        }
    }
}
