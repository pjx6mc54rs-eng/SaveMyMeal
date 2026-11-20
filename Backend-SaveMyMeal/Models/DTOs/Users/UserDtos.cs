using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.DTOs.Users
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int LoyaltyPoints { get; set; }
    }

    public class UserUpdateDto
    {
        [MaxLength(100)]
        public string? Username { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }
    }
}
