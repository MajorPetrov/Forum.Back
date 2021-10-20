using System.ComponentModel.DataAnnotations;

namespace Forum.Models.Manage
{
    public class IndexModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}