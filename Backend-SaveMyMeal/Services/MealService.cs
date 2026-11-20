using Microsoft.EntityFrameworkCore;
using Backend_SaveMyMeal.Data;
using Backend_SaveMyMeal.Models.DTOs.Meals;
using Backend_SaveMyMeal.Models.Entities;

namespace Backend_SaveMyMeal.Services
{
    public interface IMealService
    {
        Task<IEnumerable<MealResponseDto>> GetAllMealsAsync(int? categoryId = null, int? restaurantId = null, bool? onlyAvailable = true);
        Task<MealResponseDto?> GetMealByIdAsync(int id);
        Task<MealResponseDto> CreateMealAsync(MealCreateDto dto);
        Task<MealResponseDto?> UpdateMealAsync(int id, MealUpdateDto dto);
        Task<bool> DeleteMealAsync(int id);
        Task<IEnumerable<MealResponseDto>> GetMealsByRestaurantAsync(int restaurantId);
    }

    public class MealService : IMealService
    {
        private readonly AppDbContext _context;
        private readonly ISmartDiscountService _smartDiscountService;

        public MealService(AppDbContext context, ISmartDiscountService smartDiscountService)
        {
            _context = context;
            _smartDiscountService = smartDiscountService;
        }

        public async Task<IEnumerable<MealResponseDto>> GetAllMealsAsync(int? categoryId = null, int? restaurantId = null, bool? onlyAvailable = true)
        {
            var query = _context.Meals
                .Include(m => m.Restaurant)
                .Include(m => m.Category)
                .AsQueryable();

            if (onlyAvailable == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(m => m.Quantity > 0 && m.ExpiryDate > now);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            if (restaurantId.HasValue)
            {
                query = query.Where(m => m.RestaurantId == restaurantId.Value);
            }

            var meals = await query.ToListAsync();
            return meals.Select(MapToResponseDto);
        }

        public async Task<MealResponseDto?> GetMealByIdAsync(int id)
        {
            var meal = await _context.Meals
                .Include(m => m.Restaurant)
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            return meal == null ? null : MapToResponseDto(meal);
        }

        public async Task<MealResponseDto> CreateMealAsync(MealCreateDto dto)
        {
            var restaurantExists = await _context.Restaurants.AnyAsync(r => r.Id == dto.RestaurantId);
            if (!restaurantExists)
            {
                throw new KeyNotFoundException($"Le restaurant avec l'ID {dto.RestaurantId} n'existe pas.");
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
            {
                throw new KeyNotFoundException($"La catégorie avec l'ID {dto.CategoryId} n'existe pas.");
            }

            var meal = new Meal
            {
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                OriginalPrice = dto.OriginalPrice,
                DiscountedPrice = dto.DiscountedPrice,
                Quantity = dto.Quantity,
                ExpiryDate = dto.ExpiryDate,
                RestaurantId = dto.RestaurantId,
                CategoryId = dto.CategoryId
            };

            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();

            // Recharger les relations
            await _context.Entry(meal).Reference(m => m.Restaurant).LoadAsync();
            await _context.Entry(meal).Reference(m => m.Category).LoadAsync();

            return MapToResponseDto(meal);
        }

        public async Task<MealResponseDto?> UpdateMealAsync(int id, MealUpdateDto dto)
        {
            var meal = await _context.Meals
                .Include(m => m.Restaurant)
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meal == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Title))
                meal.Title = dto.Title.Trim();

            if (dto.Description != null)
                meal.Description = dto.Description.Trim();

            if (dto.OriginalPrice.HasValue)
                meal.OriginalPrice = dto.OriginalPrice.Value;

            if (dto.DiscountedPrice.HasValue)
                meal.DiscountedPrice = dto.DiscountedPrice.Value;

            if (dto.Quantity.HasValue)
                meal.Quantity = dto.Quantity.Value;

            if (dto.ExpiryDate.HasValue)
                meal.ExpiryDate = dto.ExpiryDate.Value;

            if (dto.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value);
                if (!categoryExists)
                    throw new KeyNotFoundException($"La catégorie avec l'ID {dto.CategoryId.Value} n'existe pas.");

                meal.CategoryId = dto.CategoryId.Value;
                await _context.Entry(meal).Reference(m => m.Category).LoadAsync();
            }

            await _context.SaveChangesAsync();
            return MapToResponseDto(meal);
        }

        public async Task<bool> DeleteMealAsync(int id)
        {
            var meal = await _context.Meals.FindAsync(id);
            if (meal == null) return false;

            _context.Meals.Remove(meal);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<MealResponseDto>> GetMealsByRestaurantAsync(int restaurantId)
        {
            var meals = await _context.Meals
                .Include(m => m.Restaurant)
                .Include(m => m.Category)
                .Where(m => m.RestaurantId == restaurantId)
                .ToListAsync();

            return meals.Select(MapToResponseDto);
        }

        private MealResponseDto MapToResponseDto(Meal meal)
        {
            var (smartPrice, discountPct, _) = _smartDiscountService.CalculateSmartDiscount(meal);

            return new MealResponseDto
            {
                Id = meal.Id,
                Title = meal.Title,
                Description = meal.Description,
                OriginalPrice = meal.OriginalPrice,
                DiscountedPrice = meal.DiscountedPrice,
                SmartPrice = smartPrice,
                SmartDiscountPercentage = discountPct,
                Quantity = meal.Quantity,
                ExpiryDate = meal.ExpiryDate,
                RestaurantId = meal.RestaurantId,
                RestaurantName = meal.Restaurant?.Name ?? string.Empty,
                CategoryId = meal.CategoryId,
                CategoryName = meal.Category?.Name ?? string.Empty
            };
        }
    }
}
