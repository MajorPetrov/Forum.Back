using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ForumJV.Models.ApplicationUser
{
    public class ProfileImageModel
    {
        [Required(ErrorMessage = "Please select a file.")]
        [DataType(DataType.Upload)]
        // [MaxFileSize(5 * 1024 * 1024)]
        // [AllowedExtensions(new string[] { ".jpg", ".png" })]
        public IFormFile ProfileImage { get; set; }
    }
}