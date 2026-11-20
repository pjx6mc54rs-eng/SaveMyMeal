using Microsoft.AspNetCore.Mvc;
using Backend_SaveMyMeal.Models.DTOs.Meals;
using Backend_SaveMyMeal.Services;

namespace Backend_SaveMyMeal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MealsController : ControllerBase
    {
        private readonly IMealService _mealService;

        public MealsController(IMealService mealService)
        {
            _mealService = mealService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MealResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllMeals(
            [FromQuery] int? categoryId,
            [FromQuery] int? restaurantId,
            [FromQuery] bool? onlyAvailable = true)
        {
            var meals = await _mealService.GetAllMealsAsync(categoryId, restaurantId, onlyAvailable);
            return Ok(meals);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MealResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMealById(int id)
        {
            var meal = await _mealService.GetMealByIdAsync(id);
            if (meal == null)
            {
                return NotFound(new { message = $"Plat avec l'ID {id} introuvable." });
            }

            return Ok(meal);
        }

        [HttpGet("restaurant/{restaurantId}")]
        [ProducesResponseType(typeof(IEnumerable<MealResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMealsByRestaurant(int restaurantId)
        {
            var meals = await _mealService.GetMealsByRestaurantAsync(restaurantId);
            return Ok(meals);
        }

        [HttpPost]
        [ProducesResponseType(typeof(MealResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMeal([FromBody] MealCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdMeal = await _mealService.CreateMealAsync(dto);
                return CreatedAtAction(nameof(GetMealById), new { id = createdMeal.Id }, createdMeal);
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erreur lors de la création du plat.", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(MealResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMeal(int id, [FromBody] MealUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedMeal = await _mealService.UpdateMealAsync(id, dto);
                if (updatedMeal == null)
                {
                    return NotFound(new { message = $"Plat avec l'ID {id} introuvable." });
                }

                return Ok(updatedMeal);
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erreur lors de la mise à jour.", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMeal(int id)
        {
            var deleted = await _mealService.DeleteMealAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Plat avec l'ID {id} introuvable." });
            }

            return NoContent();
        }
    }
}
