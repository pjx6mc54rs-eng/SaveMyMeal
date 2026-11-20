using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.DTOs.Restaurants
{
    public class RestaurantCreateDto
    {
        [Required(ErrorMessage = "Le nom du restaurant est requis.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse est requise.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le numéro de téléphone est requis.")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public int OwnerId { get; set; }
    }

    public class RestaurantUpdateDto
    {
        [MaxLength(150)]
        public string? Name { get; set; }

        public string? Address { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
    }

    public class RestaurantResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public string OwnerUsername { get; set; } = string.Empty;
        public int ActiveMealsCount { get; set; }
    }
}
