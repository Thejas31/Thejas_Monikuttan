using DonationFraud.API.DTOs;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DonationFraud.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _config;

        public AuthService(IUserRepository userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config = config;
        }

        public async Task<string?> RegisterAsync(RegisterRequestDto request)
        {
            if (await _userRepo.GetUserByUsernameAsync(request.Username) != null) throw new ArgumentException("Username already exists.");
            if (await _userRepo.GetUserByEmailAsync(request.Email) != null) throw new ArgumentException("Email already exists.");

            var role = await _userRepo.GetRoleByNameAsync(request.Role);
            if (role == null) 
            {
                role = new Role { Name = request.Role };
                await _userRepo.AddRoleAsync(role);
                await _userRepo.SaveChangesAsync();
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = HashPassword(request.Password),
                RoleId = role.Id
            };

            await _userRepo.AddUserAsync(user);
            await _userRepo.SaveChangesAsync();

            return GenerateJwtToken(user, role.Name);
        }

        public async Task<string?> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepo.GetUserByUsernameAsync(request.Username);
            if (user == null || user.PasswordHash != HashPassword(request.Password)) return null;

            return GenerateJwtToken(user, user.Role.Name);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private string GenerateJwtToken(User user, string roleName)
        {
            var jwtKey = _config["JwtSettings:Secret"] ?? "SuperSecretKeyForDonationFraudSystem123!";
            var jwtIssuer = _config["JwtSettings:Issuer"];
            var jwtAudience = _config["JwtSettings:Audience"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
                new Claim(ClaimTypes.Surname, user.LastName ?? ""),
                new Claim(ClaimTypes.Role, roleName)
            };

            var token = new JwtSecurityToken(jwtIssuer,
                jwtAudience,
                claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
