using Microsoft.AspNetCore.Mvc;
using Backend_SaveMyMeal.Models.DTOs.Offers;
using Backend_SaveMyMeal.Services;

namespace Backend_SaveMyMeal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OffersController : ControllerBase
    {
        private readonly ISmartDiscountService _smartDiscountService;

        public OffersController(ISmartDiscountService smartDiscountService)
        {
            _smartDiscountService = smartDiscountService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OfferResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSmartOffers(
            [FromQuery] int? categoryId,
            [FromQuery] decimal? maxPrice)
        {
            var offers = await _smartDiscountService.GetSmartOffersAsync(categoryId, maxPrice);
            return Ok(offers);
        }

        [HttpGet("{mealId}/smart-price")]
        [ProducesResponseType(typeof(OfferResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMealSmartPrice(int mealId)
        {
            var offer = await _smartDiscountService.GetOfferForMealAsync(mealId);
            if (offer == null)
            {
                return NotFound(new { message = $"Plat avec l'ID {mealId} introuvable." });
            }

            return Ok(offer);
        }
    }
}
