using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DonationFraud.API.DTOs;
using Xunit;

namespace DonationFraud.Tests.Controllers
{
    public class DonationsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public DonationsControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateDonation_WithoutAuthToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new CreateDonationDto
            {
                Amount = 100,
                CampaignId = 1,
                UserId = 1 // Assuming 1 is a valid user
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/donations", request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUserDonations_WithoutAuthToken_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/donations/user/1");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
