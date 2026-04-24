using DonationFraud.API.DTOs;
using DonationFraud.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DonationFraud.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class FraudController : ControllerBase
    {
        private readonly IFraudManagementService _fraudService;

        public FraudController(IFraudManagementService fraudService)
        {
            _fraudService = fraudService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFraudAlerts()
        {
            var alerts = await _fraudService.GetAllAlertsAsync();
            return Ok(alerts);
        }

        [HttpGet("high-risk")]
        public async Task<IActionResult> GetHighRiskAlerts()
        {
            var alerts = await _fraudService.GetHighRiskAlertsAsync();
            return Ok(alerts);
        }

        [HttpPut("{id}/review")]
        public async Task<IActionResult> ReviewFraudAlert(int id, [FromBody] ReviewFraudAlertDto request)
        {
            var adminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int adminUserId = string.IsNullOrEmpty(adminIdString) ? 0 : int.Parse(adminIdString);

            var result = await _fraudService.ReviewAlertAsync(id, request.IsApproved, request.Notes, adminUserId);
            if (!result)
            {
                return NotFound("Alert not found.");
            }

            return Ok(new { Message = "Alert reviewed successfully." });
        }
    }
}
