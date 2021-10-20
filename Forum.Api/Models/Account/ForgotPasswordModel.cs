using System.ComponentModel.DataAnnotations;

namespace Forum.Models.Account
{
    public class ForgotPasswordModel
    {
        [Required]
        [Display(Name = "Pseudo")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}