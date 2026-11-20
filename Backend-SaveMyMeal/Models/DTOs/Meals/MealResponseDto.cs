namespace Backend_SaveMyMeal.Models.DTOs.Meals
{
    public class MealResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal SmartPrice { get; set; }
        public int SmartDiscountPercentage { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsExpired => ExpiryDate <= DateTime.UtcNow;
        public double HoursUntilExpiry => Math.Round(Math.Max(0, (ExpiryDate - DateTime.UtcNow).TotalHours), 1);
    }
}
