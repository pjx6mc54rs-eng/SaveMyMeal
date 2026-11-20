using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.DTOs.Loyalty
{
    public class LoyaltyAccountDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int SavePointsBalance { get; set; }
        public decimal EuroEquivalent => (decimal)SavePointsBalance / 20; // 20 points = 1€
    }

    public class PointsRedemptionDto
    {
        [Required]
        [Range(10, 10000, ErrorMessage = "Le minimum de points à convertir est de 10.")]
        public int PointsToRedeem { get; set; }
    }

    public class PointsRewardResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PointsRedeemed { get; set; }
        public decimal DiscountAmount { get; set; }
        public int RemainingBalance { get; set; }
    }

    public class AddPointsDto
    {
        [Required]
        [Range(1, 10000)]
        public int Points { get; set; }
    }
}
