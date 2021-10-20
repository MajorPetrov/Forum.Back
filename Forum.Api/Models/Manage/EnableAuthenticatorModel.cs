using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ForumJV.Models.Manage
{
    public class EnableAuthenticatorModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "Le {0} doit comporter au moins {2} et au maximum {1} caractères.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Code de vérification")]
        public string Code { get; set; }

        [ReadOnly(true)]
        public string SharedKey { get; set; }

        public string AuthenticatorUri { get; set; }
    }
}