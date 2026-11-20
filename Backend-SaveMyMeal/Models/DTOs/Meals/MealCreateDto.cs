using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.DTOs.Meals
{
    public class MealCreateDto
    {
        [Required(ErrorMessage = "Le titre est requis.")]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 10000, ErrorMessage = "Le prix d'origine doit être supérieur à 0.")]
        public decimal OriginalPrice { get; set; }

        [Required]
        [Range(0.01, 10000, ErrorMessage = "Le prix remisé doit être supérieur à 0.")]
        public decimal DiscountedPrice { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "La quantité doit être d'au moins 1.")]
        public int Quantity { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [Required]
        public int CategoryId { get; set; }
    }
}
