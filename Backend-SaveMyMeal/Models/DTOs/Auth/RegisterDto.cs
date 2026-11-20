using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Le nom d'utilisateur est requis.")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse email est requise.")]
        [EmailAddress(ErrorMessage = "Format d'email invalide.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis.")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit comporter au moins 6 caractères.")]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Client"; // "Client", "Restaurant", "Admin"
    }
}
