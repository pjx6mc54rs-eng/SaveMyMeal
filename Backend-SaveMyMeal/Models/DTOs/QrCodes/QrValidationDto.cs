using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.DTOs.QrCodes
{
    public class QrValidationRequestDto
    {
        [Required(ErrorMessage = "Le code QR est requis.")]
        public string CodeValue { get; set; } = string.Empty;

        public int? RestaurantId { get; set; }
    }

    public class QrValidationResponseDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public string? ClientUsername { get; set; }
        public string? RestaurantName { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? Status { get; set; }
        public DateTime? ValidatedAt { get; set; }
        public List<string> ItemsSummary { get; set; } = new();
    }
}
