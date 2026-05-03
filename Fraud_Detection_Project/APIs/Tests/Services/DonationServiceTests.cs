using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using DonationFraud.API.Models;
using DonationFraud.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonationFraud.Tests.Services
{
    public class DonationServiceTests
    {
        private readonly Mock<IDonationRepository> _donationRepoMock;
        private readonly Mock<IFraudDetectionService> _fraudDetectionMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly DonationService _service;

        public DonationServiceTests()
        {
            _donationRepoMock = new Mock<IDonationRepository>();
            _fraudDetectionMock = new Mock<IFraudDetectionService>();
            _auditMock = new Mock<IAuditService>();
            var logger = new Mock<ILogger<DonationService>>();

            _donationRepoMock.Setup(r => r.AddDonationAsync(It.IsAny<Donation>())).Returns(Task.CompletedTask);
            _donationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.LogActionAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            _service = new DonationService(_donationRepoMock.Object, _fraudDetectionMock.Object, _auditMock.Object, logger.Object);
        }

        [Fact]
        public async Task ProcessDonation_NotHighRisk_ReturnsSuccess()
        {
            _fraudDetectionMock.Setup(f => f.EvaluateAndFlagDonationAsync(It.IsAny<Donation>(), It.IsAny<int>())).ReturnsAsync(false);
            var result = await _service.ProcessDonationAsync(new CreateDonationDto { CampaignId = 1, Amount = 100, UserId = 1, IpAddress = "1.2.3.4" });

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Reason);
            _donationRepoMock.Verify(r => r.AddDonationAsync(It.IsAny<Donation>()), Times.Once);
        }

        [Fact]
        public async Task ProcessDonation_HighRisk_ReturnsFailure()
        {
            _fraudDetectionMock.Setup(f => f.EvaluateAndFlagDonationAsync(It.IsAny<Donation>(), It.IsAny<int>())).ReturnsAsync(true);
            var result = await _service.ProcessDonationAsync(new CreateDonationDto { CampaignId = 1, Amount = 50000, UserId = 1, IpAddress = "1.2.3.4" });

            Assert.False(result.IsSuccess);
            Assert.Contains("blocked", result.Reason);
        }

        [Fact]
        public async Task ProcessDonation_AlwaysLogsAudit()
        {
            _fraudDetectionMock.Setup(f => f.EvaluateAndFlagDonationAsync(It.IsAny<Donation>(), It.IsAny<int>())).ReturnsAsync(false);
            await _service.ProcessDonationAsync(new CreateDonationDto { CampaignId = 1, Amount = 100, UserId = 5, IpAddress = "1.2.3.4" });

            _auditMock.Verify(a => a.LogActionAsync(It.Is<string>(s => s.Contains("100")), 5, "Donation"), Times.Once);
        }

        [Fact]
        public async Task GetUserDonations_ReturnsDtoList()
        {
            var donations = new List<Donation>
            {
                new() { Id = 1, Amount = 50, Timestamp = DateTime.UtcNow, CampaignId = 1, UserId = 1, Campaign = new Campaign { Title = "Test" } },
                new() { Id = 2, Amount = 100, Timestamp = DateTime.UtcNow, CampaignId = 1, UserId = 1, Campaign = new Campaign { Title = "Test" }, FraudFlag = new FraudFlag { Reason = "Spike", RiskLevel = API.Enums.RiskLevel.High } }
            };
            _donationRepoMock.Setup(r => r.GetDonationsByUserIdAsync(1)).ReturnsAsync(donations);

            var result = (await _service.GetUserDonationsAsync(1)).ToList();

            Assert.Equal(2, result.Count);
            Assert.False(result[0].IsFlagged);
            Assert.True(result[1].IsFlagged);
            Assert.Equal("Spike", result[1].FraudReason);
        }
    }
}
