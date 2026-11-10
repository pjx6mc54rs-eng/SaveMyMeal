using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.Entities
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Meal> Meals { get; set; } = new List<Meal>();
    }
}