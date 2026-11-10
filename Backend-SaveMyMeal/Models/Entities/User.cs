using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Client"; // Admin, Restaurant, Client

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relations
        public ICollection<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public LoyaltyAccount? LoyaltyAccount { get; set; }
    }
}