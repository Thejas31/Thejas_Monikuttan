using DonationFraud.API.Data;
using DonationFraud.API.Entities;
using DonationFraud.API.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DonationFraud.API.Services
{
    public class AuditService : IAuditService
    {
        private readonly DonationDbContext _dbContext;
        private readonly ILogger<AuditService> _logger;

        public AuditService(DonationDbContext dbContext, ILogger<AuditService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task LogActionAsync(string action, int userId, string entity)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                UserId = userId,
                Entity = entity,
                Timestamp = DateTime.UtcNow
            };

            await _dbContext.AuditLogs.AddAsync(auditLog);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Audit Log created: {Action} by User {UserId} on {Entity}", action, userId, entity);
        }
    }
}
