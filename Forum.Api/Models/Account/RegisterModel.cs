using System.ComponentModel.DataAnnotations;

namespace Forum.Models.Account
{
    public class RegisterModel
    {
        [Required]
        [StringLength(12, ErrorMessage = "Le {0} doit comporter au moins {2} et au maximum {1} caractères.", MinimumLength = 5)]
        [RegularExpression(@"^[a-zA-Z0-9_-]*$", ErrorMessage = "Le {0} ne peut comporter que des caractères alphanumériques")]
        [Display(Name = "Pseudo")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Assurez-vous d'avoir entré une adresse valide")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Le {0} doit comporter au moins {2} et au maximum {1} caractères.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessage = "Le {0} doit comporter une lettre majuscule, un minuscule et un chiffre")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe de confirmation")]
        [Compare("Password", ErrorMessage = "Le {0} et le {1} ne correspondent pas.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Charte du Forum")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Vous ne pouvez pas vous inscrire sans confirmer d'avoir lu la charte")]
        public bool TermsAndConditions { get; set; }

        [Required]
        public string CaptchaResponse { get; set; }
    }
}