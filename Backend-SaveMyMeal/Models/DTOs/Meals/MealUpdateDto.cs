using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.DTOs.Meals
{
    public class MealUpdateDto
    {
        [MaxLength(150)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Range(0.01, 10000)]
        public decimal? OriginalPrice { get; set; }

        [Range(0.01, 10000)]
        public decimal? DiscountedPrice { get; set; }

        [Range(0, 10000)]
        public int? Quantity { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public int? CategoryId { get; set; }
    }
}
