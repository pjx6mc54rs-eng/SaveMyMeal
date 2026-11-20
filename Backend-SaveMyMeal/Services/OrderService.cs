using Microsoft.EntityFrameworkCore;
using Backend_SaveMyMeal.Data;
using Backend_SaveMyMeal.Models.DTOs.Orders;
using Backend_SaveMyMeal.Models.Entities;

namespace Backend_SaveMyMeal.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrderAsync(int clientId, OrderCreateDto dto);
        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<IEnumerable<OrderResponseDto>> GetOrdersByClientAsync(int clientId);
        Task<IEnumerable<OrderResponseDto>> GetOrdersByRestaurantAsync(int restaurantId);
        Task<OrderResponseDto?> UpdateOrderStatusAsync(int orderId, string newStatus);
        Task<bool> CancelOrderAsync(int orderId, int userId, string userRole);
    }

    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IQrCodeService _qrCodeService;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ISmartDiscountService _smartDiscountService;

        public OrderService(
            AppDbContext context,
            IQrCodeService qrCodeService,
            ILoyaltyService loyaltyService,
            ISmartDiscountService smartDiscountService)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _loyaltyService = loyaltyService;
            _smartDiscountService = smartDiscountService;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(int clientId, OrderCreateDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
            {
                throw new ArgumentException("La commande doit contenir au moins un plat.");
            }

            var client = await _context.Users.FindAsync(clientId);
            if (client == null)
            {
                throw new KeyNotFoundException($"Client avec l'ID {clientId} introuvable.");
            }

            var restaurant = await _context.Restaurants.FindAsync(dto.RestaurantId);
            if (restaurant == null)
            {
                throw new KeyNotFoundException($"Restaurant avec l'ID {dto.RestaurantId} introuvable.");
            }

            var mealIds = dto.Items.Select(i => i.MealId).Distinct().ToList();
            var meals = await _context.Meals
                .Where(m => mealIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            decimal grossTotal = 0m;
            var orderItems = new List<OrderItem>();

            foreach (var item in dto.Items)
            {
                if (!meals.TryGetValue(item.MealId, out var meal))
                {
                    throw new KeyNotFoundException($"Le plat avec l'ID {item.MealId} est introuvable.");
                }

                if (meal.RestaurantId != dto.RestaurantId)
                {
                    throw new InvalidOperationException($"Le plat '{meal.Title}' n'appartient pas au restaurant sélectionné.");
                }

                if (meal.Quantity < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Stock insuffisant pour '{meal.Title}'. Restant : {meal.Quantity}, demandé : {item.Quantity}.");
                }

                if (meal.ExpiryDate <= DateTime.UtcNow)
                {
                    throw new InvalidOperationException($"Le plat '{meal.Title}' a déjà expiré et ne peut plus être réservé.");
                }

                // Déduire le stock
                meal.Quantity -= item.Quantity;

                // Calculer le prix unitaire intelligent (anti-gaspillage dynamique)
                var (unitPrice, _, _) = _smartDiscountService.CalculateSmartDiscount(meal);

                grossTotal += unitPrice * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    MealId = meal.Id,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice
                });
            }

            // Gestion de la réduction fidélité
            decimal loyaltyDiscount = 0m;
            if (dto.PointsToRedeem > 0)
            {
                var redemption = await _loyaltyService.RedeemPointsAsync(clientId, dto.PointsToRedeem);
                if (redemption.Success)
                {
                    loyaltyDiscount = redemption.DiscountAmount;
                }
            }

            decimal netTotal = Math.Max(0m, grossTotal - loyaltyDiscount);

            var order = new Order
            {
                ClientId = clientId,
                RestaurantId = dto.RestaurantId,
                OrderDate = DateTime.UtcNow,
                Status = "Confirmed",
                TotalPrice = netTotal,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Génération du QR Code unique pour le retrait en restaurant
            var qrCode = await _qrCodeService.GenerateQrCodeAsync(order.Id);

            // Attribution de points fidélité SavePoints : 1 point par euro dépensé + 5 points bonus anti-gaspi
            int earnedPoints = (int)Math.Floor(netTotal) + 5;
            if (earnedPoints > 0)
            {
                await _loyaltyService.AddPointsAsync(clientId, earnedPoints);
            }

            return MapToResponseDto(order, qrCode.CodeValue, qrCode.IsUsed, loyaltyDiscount);
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Restaurant)
                .Include(o => o.QrCode)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Meal)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;

            return MapToResponseDto(order, order.QrCode?.CodeValue, order.QrCode?.IsUsed ?? false);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetOrdersByClientAsync(int clientId)
        {
            var orders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Restaurant)
                .Include(o => o.QrCode)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Meal)
                .Where(o => o.ClientId == clientId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(o => MapToResponseDto(o, o.QrCode?.CodeValue, o.QrCode?.IsUsed ?? false));
        }

        public async Task<IEnumerable<OrderResponseDto>> GetOrdersByRestaurantAsync(int restaurantId)
        {
            var orders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Restaurant)
                .Include(o => o.QrCode)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Meal)
                .Where(o => o.RestaurantId == restaurantId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(o => MapToResponseDto(o, o.QrCode?.CodeValue, o.QrCode?.IsUsed ?? false));
        }

        public async Task<OrderResponseDto?> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Restaurant)
                .Include(o => o.QrCode)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Meal)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return null;

            order.Status = newStatus;
            await _context.SaveChangesAsync();

            return MapToResponseDto(order, order.QrCode?.CodeValue, order.QrCode?.IsUsed ?? false);
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId, string userRole)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Meal)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            if (order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Une commande déjà récupérée ne peut pas être annulée.");
            }

            if (order.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Restituer les stocks des plats
            foreach (var item in order.OrderItems)
            {
                if (item.Meal != null)
                {
                    item.Meal.Quantity += item.Quantity;
                }
            }

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return true;
        }

        private static OrderResponseDto MapToResponseDto(
            Order order,
            string? qrCode = null,
            bool isQrCodeUsed = false,
            decimal loyaltyDiscount = 0m)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                ClientId = order.ClientId,
                ClientUsername = order.Client?.Username ?? string.Empty,
                RestaurantId = order.RestaurantId,
                RestaurantName = order.Restaurant?.Name ?? string.Empty,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalPrice = order.TotalPrice,
                LoyaltyDiscount = loyaltyDiscount,
                QrCode = qrCode,
                IsQrCodeUsed = isQrCodeUsed,
                Items = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    Id = oi.Id,
                    MealId = oi.MealId,
                    MealTitle = oi.Meal?.Title ?? string.Empty,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }
    }
}
