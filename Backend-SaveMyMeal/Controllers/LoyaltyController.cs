using Microsoft.AspNetCore.Mvc;
using Backend_SaveMyMeal.Models.DTOs.Loyalty;
using Backend_SaveMyMeal.Services;

namespace Backend_SaveMyMeal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyService _loyaltyService;

        public LoyaltyController(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService;
        }

        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(LoyaltyAccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLoyaltyAccount(int userId)
        {
            try
            {
                var account = await _loyaltyService.GetAccountDtoByUserIdAsync(userId);
                if (account == null)
                {
                    return NotFound(new { message = $"Compte fidélité pour l'utilisateur {userId} introuvable." });
                }

                return Ok(account);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{userId}/redeem")]
        [ProducesResponseType(typeof(PointsRewardResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RedeemPoints(int userId, [FromBody] PointsRedemptionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _loyaltyService.RedeemPointsAsync(userId, dto.PointsToRedeem);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{userId}/add")]
        [ProducesResponseType(typeof(LoyaltyAccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddPoints(int userId, [FromBody] AddPointsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var account = await _loyaltyService.AddPointsAsync(userId, dto.Points);
                return Ok(new LoyaltyAccountDto
                {
                    Id = account.Id,
                    UserId = account.UserId,
                    Username = account.User?.Username ?? string.Empty,
                    SavePointsBalance = account.SavePointsBalance
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
