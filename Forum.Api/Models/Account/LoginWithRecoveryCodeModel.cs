using System.ComponentModel.DataAnnotations;

namespace ForumJV.Models.Account
{
    public class LoginWithRecoveryCodeModel
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Code de récupération")]
        public string RecoveryCode { get; set; }
    }
}