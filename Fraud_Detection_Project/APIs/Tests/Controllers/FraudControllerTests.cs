using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DonationFraud.API.DTOs;
using Xunit;

namespace DonationFraud.Tests.Controllers
{
    public class FraudControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public FraudControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllFraudAlerts_WithoutAuthToken_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/fraud");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetHighRiskAlerts_WithoutAuthToken_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/fraud/high-risk");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ReviewFraudAlert_WithoutAuthToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new ReviewFraudAlertDto
            {
                IsApproved = true,
                Notes = "Reviewed"
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/fraud/1/review", request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
