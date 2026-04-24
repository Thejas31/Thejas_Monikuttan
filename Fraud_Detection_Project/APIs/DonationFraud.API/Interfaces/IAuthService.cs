using DonationFraud.API.DTOs;
using System.Threading.Tasks;

namespace DonationFraud.API.Interfaces
{
    public interface IAuthService
    {
        Task<string?> RegisterAsync(RegisterRequestDto request);
        Task<string?> LoginAsync(LoginRequestDto request);
    }
}
