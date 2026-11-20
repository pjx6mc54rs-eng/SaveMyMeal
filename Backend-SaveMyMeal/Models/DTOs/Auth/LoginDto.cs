using System.ComponentModel.DataAnnotations;

namespace Backend_SaveMyMeal.Models.DTOs.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "L'email ou le nom d'utilisateur est requis.")]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis.")]
        public string Password { get; set; } = string.Empty;
    }
}
