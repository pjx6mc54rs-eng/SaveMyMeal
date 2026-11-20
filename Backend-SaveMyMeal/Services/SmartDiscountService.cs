using Microsoft.EntityFrameworkCore;
using Backend_SaveMyMeal.Data;
using Backend_SaveMyMeal.Models.DTOs.Offers;
using Backend_SaveMyMeal.Models.Entities;

namespace Backend_SaveMyMeal.Services
{
    public interface ISmartDiscountService
    {
        (decimal SmartPrice, int DiscountPercentage, string UrgencyLevel) CalculateSmartDiscount(Meal meal);
        Task<IEnumerable<OfferResponseDto>> GetSmartOffersAsync(int? categoryId = null, decimal? maxPrice = null);
        Task<OfferResponseDto?> GetOfferForMealAsync(int mealId);
    }

    public class SmartDiscountService : ISmartDiscountService
    {
        private readonly AppDbContext _context;

        public SmartDiscountService(AppDbContext context)
        {
            _context = context;
        }

        public (decimal SmartPrice, int DiscountPercentage, string UrgencyLevel) CalculateSmartDiscount(Meal meal)
        {
            var now = DateTime.UtcNow;
            var timeRemaining = meal.ExpiryDate - now;
            var hours = timeRemaining.TotalHours;

            decimal basePrice = meal.OriginalPrice;
            decimal regularDiscounted = meal.DiscountedPrice > 0 ? meal.DiscountedPrice : basePrice;

            if (hours <= 0)
            {
                return (regularDiscounted, 0, "Expiré");
            }

            decimal smartPrice;
            string urgency;

            if (hours <= 1.5)
            {
                // Vente Flash / Urgence critique anti-gaspillage : 75% de remise
                urgency = "Flash Sale";
                smartPrice = Math.Round(basePrice * 0.25m, 2);
            }
            else if (hours <= 3.0)
            {
                // Urgence haute : 60% de remise
                urgency = "Critique";
                smartPrice = Math.Round(basePrice * 0.40m, 2);
            }
            else if (hours <= 6.0)
            {
                // Urgence modérée : 50% de remise
                urgency = "Modéré";
                smartPrice = Math.Round(basePrice * 0.50m, 2);
            }
            else
            {
                // Remise standard du panier
                urgency = "Standard";
                smartPrice = regularDiscounted;
            }

            // Le prix intelligent ne doit pas dépasser le prix remisé standard du commerçant
            if (smartPrice > regularDiscounted)
            {
                smartPrice = regularDiscounted;
            }

            // Prix plancher symbolique anti-gaspi (1.00€ minimum sauf si original était inférieur)
            if (smartPrice < 1.00m && basePrice >= 1.00m)
            {
                smartPrice = 1.00m;
            }

            // Calcul du pourcentage effectif de remise
            int discountPct = 0;
            if (basePrice > 0)
            {
                discountPct = (int)Math.Round(((basePrice - smartPrice) / basePrice) * 100);
            }

            return (smartPrice, Math.Max(0, discountPct), urgency);
        }

        public async Task<IEnumerable<OfferResponseDto>> GetSmartOffersAsync(int? categoryId = null, decimal? maxPrice = null)
        {
            var now = DateTime.UtcNow;

            var query = _context.Meals
                .Include(m => m.Restaurant)
                .Include(m => m.Category)
                .Where(m => m.Quantity > 0 && m.ExpiryDate > now);

            if (categoryId.HasValue)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            var meals = await query.ToListAsync();

            var offers = meals.Select(meal =>
            {
                var (smartPrice, discountPct, urgency) = CalculateSmartDiscount(meal);
                var hoursRemaining = Math.Round((meal.ExpiryDate - now).TotalHours, 1);

                return new OfferResponseDto
                {
                    MealId = meal.Id,
                    Title = meal.Title,
                    Description = meal.Description,
                    OriginalPrice = meal.OriginalPrice,
                    RegularDiscountedPrice = meal.DiscountedPrice,
                    SmartPrice = smartPrice,
                    DiscountPercentage = discountPct,
                    AvailableQuantity = meal.Quantity,
                    ExpiryDate = meal.ExpiryDate,
                    HoursRemaining = Math.Max(0, hoursRemaining),
                    UrgencyLevel = urgency,
                    RestaurantId = meal.RestaurantId,
                    RestaurantName = meal.Restaurant?.Name ?? string.Empty,
                    RestaurantAddress = meal.Restaurant?.Address ?? string.Empty,
                    CategoryId = meal.CategoryId,
                    CategoryName = meal.Category?.Name ?? string.Empty
                };
            });

            if (maxPrice.HasValue)
            {
                offers = offers.Where(o => o.SmartPrice <= maxPrice.Value);
            }

            // Trier par urgence d'expiration (les plus proches d'expirer en premier)
            return offers.OrderBy(o => o.HoursRemaining);
        }

        public async Task<OfferResponseDto?> GetOfferForMealAsync(int mealId)
        {
            var meal = await _context.Meals
                .Include(m => m.Restaurant)
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == mealId);

            if (meal == null) return null;

            var (smartPrice, discountPct, urgency) = CalculateSmartDiscount(meal);
            var now = DateTime.UtcNow;
            var hoursRemaining = Math.Round((meal.ExpiryDate - now).TotalHours, 1);

            return new OfferResponseDto
            {
                MealId = meal.Id,
                Title = meal.Title,
                Description = meal.Description,
                OriginalPrice = meal.OriginalPrice,
                RegularDiscountedPrice = meal.DiscountedPrice,
                SmartPrice = smartPrice,
                DiscountPercentage = discountPct,
                AvailableQuantity = meal.Quantity,
                ExpiryDate = meal.ExpiryDate,
                HoursRemaining = Math.Max(0, hoursRemaining),
                UrgencyLevel = urgency,
                RestaurantId = meal.RestaurantId,
                RestaurantName = meal.Restaurant?.Name ?? string.Empty,
                RestaurantAddress = meal.Restaurant?.Address ?? string.Empty,
                CategoryId = meal.CategoryId,
                CategoryName = meal.Category?.Name ?? string.Empty
            };
        }
    }
}
