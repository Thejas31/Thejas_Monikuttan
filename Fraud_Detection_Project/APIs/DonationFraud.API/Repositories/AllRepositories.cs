using DonationFraud.API.Data;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DonationFraud.API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DonationDbContext _context;
        public UserRepository(DonationDbContext context) => _context = context;

        public async Task<User?> GetUserByIdAsync(int id) => await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        public async Task<User?> GetUserByEmailAsync(string email) => await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
        public async Task<User?> GetUserByUsernameAsync(string username) => await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);
        public async Task<Role?> GetRoleByNameAsync(string name) => await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
        public async Task AddRoleAsync(Role role) => await _context.Roles.AddAsync(role);
        public async Task AddUserAsync(User user) => await _context.Users.AddAsync(user);
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }

    public class CampaignRepository : ICampaignRepository
    {
        private readonly DonationDbContext _context;
        public CampaignRepository(DonationDbContext context) => _context = context;

        public async Task<Campaign?> GetCampaignByIdAsync(int id) => await _context.Campaigns
            .Include(c => c.Donations)
                .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(c => c.Id == id);
        public async Task<IEnumerable<Campaign>> GetAllCampaignsAsync() => await _context.Campaigns
            .Include(c => c.Donations)
                .ThenInclude(d => d.User)
            .ToListAsync();
        public async Task AddCampaignAsync(Campaign campaign) => await _context.Campaigns.AddAsync(campaign);
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }

    public class DonationRepository : IDonationRepository
    {
        private readonly DonationDbContext _context;
        public DonationRepository(DonationDbContext context) => _context = context;

        public async Task<Donation?> GetDonationByIdAsync(int id) => await _context.Donations.FindAsync(id);
        public async Task<IEnumerable<Donation>> GetDonationsByUserIdAsync(int userId) => await _context.Donations
            .Include(d => d.Campaign)
            .Include(d => d.FraudFlag)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.Timestamp)
            .ToListAsync();
        public async Task<IEnumerable<Donation>> GetUserDonationsInTimespanAsync(int userId, TimeSpan timeSpan)
        {
            var cutoff = DateTime.UtcNow.Subtract(timeSpan);
            return await _context.Donations.Where(d => d.UserId == userId && d.Timestamp >= cutoff).ToListAsync();
        }
        public async Task AddDonationAsync(Donation donation) => await _context.Donations.AddAsync(donation);
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }

    public class FraudFlagRepository : IFraudFlagRepository
    {
        private readonly DonationDbContext _context;
        public FraudFlagRepository(DonationDbContext context) => _context = context;

        public async Task<FraudFlag?> GetFraudFlagByIdAsync(int id) => await _context.FraudFlags.Include(f => f.Donation).ThenInclude(d => d.User).FirstOrDefaultAsync(f => f.Id == id);
        public async Task<IEnumerable<FraudFlag>> GetAllFlagsAsync() => await _context.FraudFlags.Include(f => f.Donation).ThenInclude(d => d.User).ToListAsync();
        public async Task<IEnumerable<FraudFlag>> GetHighRiskFlagsAsync() => await _context.FraudFlags.Where(f => f.RiskLevel == Enums.RiskLevel.High).Include(f => f.Donation).ThenInclude(d => d.User).ToListAsync();
        public async Task AddFraudFlagAsync(FraudFlag flag) => await _context.FraudFlags.AddAsync(flag);
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
