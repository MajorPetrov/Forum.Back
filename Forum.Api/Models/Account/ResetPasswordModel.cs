using System.ComponentModel.DataAnnotations;

namespace Forum.Models.Account
{
    public class ResetPasswordModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Le {0} doit comporter au moins {2} et au maximum {1} caractères.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W]).{8,}$", ErrorMessage = "Le {0} doit comporter une lettre majuscule, un minuscule, un chiffre et un caractère spécial")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe de confirmation")]
        [Compare("Password", ErrorMessage = "Le {0} et le {1} ne correspondent pas.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Code { get; set; }
    }
}