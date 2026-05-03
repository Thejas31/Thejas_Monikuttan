using DonationFraud.API.DTOs;
using DonationFraud.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DonationFraud.API.Interfaces
{
    public interface IDonationService
    {
        Task<ProcessDonationResult> ProcessDonationAsync(CreateDonationDto request);
        Task<IEnumerable<DonationResponseDto>> GetUserDonationsAsync(int userId);
    }
}
