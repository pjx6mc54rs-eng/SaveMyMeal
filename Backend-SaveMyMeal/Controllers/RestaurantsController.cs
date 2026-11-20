using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_SaveMyMeal.Data;
using Backend_SaveMyMeal.Models.DTOs.Restaurants;
using Backend_SaveMyMeal.Models.Entities;
using Backend_SaveMyMeal.Services;

namespace Backend_SaveMyMeal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMealService _mealService;
        private readonly IOrderService _orderService;

        public RestaurantsController(
            AppDbContext context,
            IMealService mealService,
            IOrderService orderService)
        {
            _context = context;
            _mealService = mealService;
            _orderService = orderService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RestaurantResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllRestaurants()
        {
            var restaurants = await _context.Restaurants
                .Include(r => r.Owner)
                .Include(r => r.Meals)
                .Select(r => new RestaurantResponseDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Address = r.Address,
                    PhoneNumber = r.PhoneNumber,
                    OwnerId = r.OwnerId,
                    OwnerUsername = r.Owner != null ? r.Owner.Username : string.Empty,
                    ActiveMealsCount = r.Meals.Count(m => m.Quantity > 0 && m.ExpiryDate > DateTime.UtcNow)
                })
                .ToListAsync();

            return Ok(restaurants);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RestaurantResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRestaurantById(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.Owner)
                .Include(r => r.Meals)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound(new { message = $"Restaurant avec l'ID {id} introuvable." });
            }

            var dto = new RestaurantResponseDto
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Address = restaurant.Address,
                PhoneNumber = restaurant.PhoneNumber,
                OwnerId = restaurant.OwnerId,
                OwnerUsername = restaurant.Owner?.Username ?? string.Empty,
                ActiveMealsCount = restaurant.Meals.Count(m => m.Quantity > 0 && m.ExpiryDate > DateTime.UtcNow)
            };

            return Ok(dto);
        }

        [HttpPost]
        [ProducesResponseType(typeof(RestaurantResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRestaurant([FromBody] RestaurantCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ownerExists = await _context.Users.AnyAsync(u => u.Id == dto.OwnerId);
            if (!ownerExists)
            {
                return BadRequest(new { message = $"Le propriétaire avec l'ID {dto.OwnerId} n'existe pas." });
            }

            var restaurant = new Restaurant
            {
                Name = dto.Name.Trim(),
                Address = dto.Address.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                OwnerId = dto.OwnerId
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
            await _context.Entry(restaurant).Reference(r => r.Owner).LoadAsync();

            var responseDto = new RestaurantResponseDto
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Address = restaurant.Address,
                PhoneNumber = restaurant.PhoneNumber,
                OwnerId = restaurant.OwnerId,
                OwnerUsername = restaurant.Owner?.Username ?? string.Empty,
                ActiveMealsCount = 0
            };

            return CreatedAtAction(nameof(GetRestaurantById), new { id = restaurant.Id }, responseDto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(RestaurantResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRestaurant(int id, [FromBody] RestaurantUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.Owner)
                .Include(r => r.Meals)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound(new { message = $"Restaurant avec l'ID {id} introuvable." });
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
                restaurant.Name = dto.Name.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Address))
                restaurant.Address = dto.Address.Trim();

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                restaurant.PhoneNumber = dto.PhoneNumber.Trim();

            await _context.SaveChangesAsync();

            var responseDto = new RestaurantResponseDto
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Address = restaurant.Address,
                PhoneNumber = restaurant.PhoneNumber,
                OwnerId = restaurant.OwnerId,
                OwnerUsername = restaurant.Owner?.Username ?? string.Empty,
                ActiveMealsCount = restaurant.Meals.Count(m => m.Quantity > 0 && m.ExpiryDate > DateTime.UtcNow)
            };

            return Ok(responseDto);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRestaurant(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null)
            {
                return NotFound(new { message = $"Restaurant avec l'ID {id} introuvable." });
            }

            _context.Restaurants.Remove(restaurant);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id}/meals")]
        public async Task<IActionResult> GetRestaurantMeals(int id)
        {
            var meals = await _mealService.GetMealsByRestaurantAsync(id);
            return Ok(meals);
        }

        [HttpGet("{id}/orders")]
        public async Task<IActionResult> GetRestaurantOrders(int id)
        {
            var orders = await _orderService.GetOrdersByRestaurantAsync(id);
            return Ok(orders);
        }
    }
}
