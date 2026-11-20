namespace Backend_SaveMyMeal.Models.DTOs.Orders
{
    public class OrderItemResponseDto
    {
        public int Id { get; set; }
        public int MealId { get; set; }
        public string MealTitle { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal => UnitPrice * Quantity;
    }

    public class OrderResponseDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientUsername { get; set; } = string.Empty;
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public decimal LoyaltyDiscount { get; set; }
        public string? QrCode { get; set; }
        public bool IsQrCodeUsed { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }
}
