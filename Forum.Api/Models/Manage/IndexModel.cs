using System.ComponentModel.DataAnnotations;

namespace ForumJV.Models.Manage
{
    public class IndexModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}