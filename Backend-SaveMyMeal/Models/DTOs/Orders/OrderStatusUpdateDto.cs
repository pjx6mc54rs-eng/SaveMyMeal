using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.DTOs.Orders
{
    public class OrderStatusUpdateDto
    {
        [Required(ErrorMessage = "Le statut est requis.")]
        public string Status { get; set; } = string.Empty; // Pending, Confirmed, ReadyForPickup, Completed, Cancelled
    }
}
