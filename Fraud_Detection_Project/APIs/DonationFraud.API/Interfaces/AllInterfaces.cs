using DonationFraud.API.Entities;
using DonationFraud.API.DTOs;
using DonationFraud.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace DonationFraud.API.Interfaces
{
    // ====== Repositories ======
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<Role?> GetRoleByNameAsync(string name);
        Task AddRoleAsync(Role role);
        Task AddUserAsync(User user);
        Task SaveChangesAsync();
    }

    public interface ICampaignRepository
    {
        Task<Campaign?> GetCampaignByIdAsync(int id);
        Task<IEnumerable<Campaign>> GetAllCampaignsAsync();
        Task AddCampaignAsync(Campaign campaign);
        Task SaveChangesAsync();
    }

    public interface IDonationRepository
    {
        Task<Donation?> GetDonationByIdAsync(int id);
        Task<IEnumerable<Donation>> GetDonationsByUserIdAsync(int userId);
        Task<IEnumerable<Donation>> GetUserDonationsInTimespanAsync(int userId, TimeSpan timeSpan);
        Task AddDonationAsync(Donation donation);
        Task SaveChangesAsync();
    }

    public interface IFraudFlagRepository
    {
        Task<FraudFlag?> GetFraudFlagByIdAsync(int id);
        Task<IEnumerable<FraudFlag>> GetAllFlagsAsync();
        Task<IEnumerable<FraudFlag>> GetHighRiskFlagsAsync();
        Task AddFraudFlagAsync(FraudFlag flag);
        Task SaveChangesAsync();
    }

}
