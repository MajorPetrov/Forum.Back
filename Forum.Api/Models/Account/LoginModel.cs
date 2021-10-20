using System.ComponentModel.DataAnnotations;

namespace Forum.Models.Account
{
    public class LoginModel
    {
        [Required]
        [Display(Name = "Pseudo")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }
    }
}