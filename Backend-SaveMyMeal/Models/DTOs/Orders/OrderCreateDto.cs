using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.DTOs.Orders
{
    public class OrderItemCreateDto
    {
        [Required]
        public int MealId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "La quantité doit être entre 1 et 100.")]
        public int Quantity { get; set; }
    }

    public class OrderCreateDto
    {
        public int ClientId { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "La commande doit contenir au moins un plat.")]
        public List<OrderItemCreateDto> Items { get; set; } = new();

        public int PointsToRedeem { get; set; } = 0;
    }
}
