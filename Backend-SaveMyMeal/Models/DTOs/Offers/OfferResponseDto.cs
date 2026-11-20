namespace Backend_SaveMyMeal.Models.DTOs.Offers
{
    public class OfferResponseDto
    {
        public int MealId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal RegularDiscountedPrice { get; set; }
        public decimal SmartPrice { get; set; }
        public int DiscountPercentage { get; set; }
        public int AvailableQuantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public double HoursRemaining { get; set; }
        public string UrgencyLevel { get; set; } = string.Empty; // "Flash Sale", "Critique", "Modéré", "Standard"
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public string RestaurantAddress { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
