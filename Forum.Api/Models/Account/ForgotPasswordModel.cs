using System.ComponentModel.DataAnnotations;

namespace ForumJV.Models.Account
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