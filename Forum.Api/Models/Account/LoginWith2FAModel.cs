using System.ComponentModel.DataAnnotations;

namespace ForumJV.Models.Account
{
    public class LoginWith2FAModel
    {
        [Required]
        [Display(Name = "Pseudo")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Code de sécurité")]
        public string TwoFactorCode { get; set; }

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }

        [Display(Name = "Se souvenir de cet authenticateur")]
        public bool RememberMachine { get; set; }
    }
}