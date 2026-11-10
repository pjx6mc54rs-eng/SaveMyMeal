using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_SaveMyMeal.Models.Entities
{
    public class QrCode
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [Required]
        public string CodeValue { get; set; } = string.Empty;

        public bool IsUsed { get; set; } = false;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}