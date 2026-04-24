using System.Threading.Tasks;

namespace DonationFraud.API.Interfaces
{
    public interface IAuditService
    {
        Task LogActionAsync(string action, int userId, string entity);
    }
}
