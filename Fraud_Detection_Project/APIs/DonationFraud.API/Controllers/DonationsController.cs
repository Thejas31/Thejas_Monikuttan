using DonationFraud.API.DTOs;
using DonationFraud.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DonationFraud.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DonationsController : ControllerBase
    {
        private readonly IDonationService _donationService;

        public DonationsController(IDonationService donationService)
        {
            _donationService = donationService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDonation([FromBody] CreateDonationDto request)
        {
            // Extract user IP and UserId from the context
            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            request.UserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _donationService.ProcessDonationAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(new { Message = "Donation flagged or failed.", Reason = result.Reason });
            }

            return Ok(new { Message = "Donation successful.", DonationId = result.DonationId });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserDonations(int userId)
        {
            var callingUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (callingUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var donations = await _donationService.GetUserDonationsAsync(userId);
            return Ok(donations);
        }
    }
}
