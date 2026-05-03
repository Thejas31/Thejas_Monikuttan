using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using DonationFraud.API.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DonationFraud.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            var configValues = new Dictionary<string, string?>
            {
                { "JwtSettings:Secret", "SuperSecretKeyForDonationFraudSystem123!" },
                { "JwtSettings:Issuer", "DonationFraudAPI" },
                { "JwtSettings:Audience", "DonationFraudUsers" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
            _authService = new AuthService(_userRepoMock.Object, config);
        }

        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        }

        [Fact]
        public async Task Register_NewUser_ReturnsJwtToken()
        {
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("newuser")).ReturnsAsync((User?)null);
            _userRepoMock.Setup(r => r.GetUserByEmailAsync("new@test.com")).ReturnsAsync((User?)null);
            _userRepoMock.Setup(r => r.GetRoleByNameAsync("User")).ReturnsAsync(new Role { Id = 1, Name = "User" });
            _userRepoMock.Setup(r => r.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _userRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var token = await _authService.RegisterAsync(new RegisterRequestDto { Username = "newuser", Email = "new@test.com", Password = "pass123", Role = "User" });

            Assert.NotNull(token);
            Assert.NotEmpty(token);
            _userRepoMock.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task Register_DuplicateUsername_ThrowsArgumentException()
        {
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("existing")).ReturnsAsync(new User { Id = 1, Username = "existing", Email = "e@t.com", PasswordHash = "h", RoleId = 1 });
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(new RegisterRequestDto { Username = "existing", Email = "n@t.com", Password = "p" }));
            Assert.Contains("Username already exists", ex.Message);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ThrowsArgumentException()
        {
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("new")).ReturnsAsync((User?)null);
            _userRepoMock.Setup(r => r.GetUserByEmailAsync("dup@t.com")).ReturnsAsync(new User { Id = 1, Username = "o", Email = "dup@t.com", PasswordHash = "h", RoleId = 1 });
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(new RegisterRequestDto { Username = "new", Email = "dup@t.com", Password = "p" }));
            Assert.Contains("Email already exists", ex.Message);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            var user = new User { Id = 1, Username = "testuser", Email = "t@t.com", PasswordHash = HashPassword("pass123"), RoleId = 1, Role = new Role { Id = 1, Name = "User" } };
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);
            var token = await _authService.LoginAsync(new LoginRequestDto { Username = "testuser", Password = "pass123" });
            Assert.NotNull(token);
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsNull()
        {
            var user = new User { Id = 1, Username = "testuser", Email = "t@t.com", PasswordHash = HashPassword("correct"), RoleId = 1, Role = new Role { Id = 1, Name = "User" } };
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);
            var token = await _authService.LoginAsync(new LoginRequestDto { Username = "testuser", Password = "wrong" });
            Assert.Null(token);
        }

        [Fact]
        public async Task Login_NonExistentUser_ReturnsNull()
        {
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("nobody")).ReturnsAsync((User?)null);
            var token = await _authService.LoginAsync(new LoginRequestDto { Username = "nobody", Password = "p" });
            Assert.Null(token);
        }
    }
}
