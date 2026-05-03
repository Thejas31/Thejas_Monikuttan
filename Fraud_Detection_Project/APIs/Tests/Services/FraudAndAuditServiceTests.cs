using DonationFraud.API.Entities;
using DonationFraud.API.Enums;
using DonationFraud.API.Interfaces;
using DonationFraud.API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DonationFraud.Tests.Services
{
    public class FraudManagementServiceTests
    {
        private readonly Mock<IFraudFlagRepository> _repoMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly FraudManagementService _service;

        public FraudManagementServiceTests()
        {
            _repoMock = new Mock<IFraudFlagRepository>();
            _auditMock = new Mock<IAuditService>();
            _auditMock.Setup(a => a.LogActionAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _service = new FraudManagementService(_repoMock.Object, _auditMock.Object);
        }

        [Fact]
        public async Task GetAllAlerts_ReturnsDtos()
        {
            var flags = new List<FraudFlag>
            {
                new() { Id = 1, DonationId = 10, RiskScore = 80, RiskLevel = RiskLevel.High, Reason = "Spike",
                    Donation = new Donation { Id = 10, Amount = 50000, Timestamp = DateTime.UtcNow, UserId = 1, User = new User { Id = 1, Username = "donor1" } } }
            };
            _repoMock.Setup(r => r.GetAllFlagsAsync()).ReturnsAsync(flags);

            var result = (await _service.GetAllAlertsAsync()).ToList();

            Assert.Single(result);
            Assert.Equal(50000, result[0].DonationAmount);
            Assert.Equal("donor1", result[0].DonorUsername);
            Assert.Equal("High", result[0].RiskLevel);
        }

        [Fact]
        public async Task ReviewAlert_Found_UpdatesAndReturnsTrue()
        {
            var flag = new FraudFlag { Id = 1, DonationId = 10, RiskScore = 80, RiskLevel = RiskLevel.High, Reason = "Spike",
                Donation = new Donation { Id = 10, Amount = 50000, UserId = 1, User = new User { Id = 1, Username = "d" } } };
            _repoMock.Setup(r => r.GetFraudFlagByIdAsync(1)).ReturnsAsync(flag);

            var result = await _service.ReviewAlertAsync(1, true, "Looks legit", 99);

            Assert.True(result);
            Assert.True(flag.IsApproved);
            Assert.Equal("Looks legit", flag.AdminNotes);
            _auditMock.Verify(a => a.LogActionAsync(It.Is<string>(s => s.Contains("Reviewed")), 99, "FraudFlag"), Times.Once);
        }

        [Fact]
        public async Task ReviewAlert_NotFound_ReturnsFalse()
        {
            _repoMock.Setup(r => r.GetFraudFlagByIdAsync(999)).ReturnsAsync((FraudFlag?)null);
            var result = await _service.ReviewAlertAsync(999, true, "Notes", 1);
            Assert.False(result);
        }
    }

    public class FraudDetectionServiceTests
    {
        private readonly Mock<API.FraudEngine.IFraudEvaluator> _evaluatorMock;
        private readonly Mock<IFraudFlagRepository> _flagRepoMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly FraudDetectionService _service;

        public FraudDetectionServiceTests()
        {
            _evaluatorMock = new Mock<API.FraudEngine.IFraudEvaluator>();
            _flagRepoMock = new Mock<IFraudFlagRepository>();
            _auditMock = new Mock<IAuditService>();
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FraudDetectionService>>();

            _flagRepoMock.Setup(r => r.AddFraudFlagAsync(It.IsAny<FraudFlag>())).Returns(Task.CompletedTask);
            _flagRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.LogActionAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            _service = new FraudDetectionService(_evaluatorMock.Object, _flagRepoMock.Object, _auditMock.Object, logger.Object);
        }

        [Fact]
        public async Task Evaluate_LowRisk_ReturnsFalse()
        {
            _evaluatorMock.Setup(e => e.EvaluateDonationAsync(It.IsAny<Donation>(), It.IsAny<int>()))
                .ReturnsAsync(new API.FraudEngine.FraudResult { TotalRiskScore = 10, RiskLevel = RiskLevel.Low, CombinedReasons = "" });

            var donation = new Donation { Id = 1, Amount = 50, UserId = 1 };
            var result = await _service.EvaluateAndFlagDonationAsync(donation, 1);

            Assert.False(result);
            _flagRepoMock.Verify(r => r.AddFraudFlagAsync(It.IsAny<FraudFlag>()), Times.Never);
        }

        [Fact]
        public async Task Evaluate_MediumRisk_CreatesFlagButReturnsFalse()
        {
            _evaluatorMock.Setup(e => e.EvaluateDonationAsync(It.IsAny<Donation>(), It.IsAny<int>()))
                .ReturnsAsync(new API.FraudEngine.FraudResult { TotalRiskScore = 40, RiskLevel = RiskLevel.Medium, CombinedReasons = "HighFrequency" });

            var donation = new Donation { Id = 2, Amount = 50, UserId = 1 };
            var result = await _service.EvaluateAndFlagDonationAsync(donation, 1);

            Assert.False(result); // Medium risk flags but doesn't block
            _flagRepoMock.Verify(r => r.AddFraudFlagAsync(It.IsAny<FraudFlag>()), Times.Once);
        }

        [Fact]
        public async Task Evaluate_HighRisk_CreatesFlagAndReturnsTrue()
        {
            _evaluatorMock.Setup(e => e.EvaluateDonationAsync(It.IsAny<Donation>(), It.IsAny<int>()))
                .ReturnsAsync(new API.FraudEngine.FraudResult { TotalRiskScore = 80, RiskLevel = RiskLevel.High, CombinedReasons = "Spike" });

            var donation = new Donation { Id = 3, Amount = 50000, UserId = 1 };
            var result = await _service.EvaluateAndFlagDonationAsync(donation, 1);

            Assert.True(result); // High risk blocks the donation
            _flagRepoMock.Verify(r => r.AddFraudFlagAsync(It.IsAny<FraudFlag>()), Times.Once);
            _auditMock.Verify(a => a.LogActionAsync(It.IsAny<string>(), 1, "FraudFlag"), Times.Once);
        }
    }

    public class AuditServiceTests
    {
        [Fact]
        public async Task LogAction_CreatesAuditLogEntry()
        {
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<API.Data.DonationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var db = new API.Data.DonationDbContext(options);
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<AuditService>>();
            var service = new AuditService(db, logger.Object);

            await service.LogActionAsync("Test action", 1, "TestEntity");

            Assert.Single(db.AuditLogs);
            var log = db.AuditLogs.First();
            Assert.Equal("Test action", log.Action);
            Assert.Equal(1, log.UserId);
            Assert.Equal("TestEntity", log.Entity);
        }
    }
}
