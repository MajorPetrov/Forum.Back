using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ForumJV.Data;

namespace ForumJV.Models.Post
{
    public class NewPostModel
    {
        public int Id { get; set; }

        [StringLength(99, ErrorMessage = "Le {0} doit comporter au moins {2} et au maximum {1} caractères.", MinimumLength = 1)]
        public string Title { get; set; }

        [StringLength(65536, ErrorMessage = "Le {0} doit comporter au moins {2} et au maximum {1} caractères.", MinimumLength = 3)]
        public string Content { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Color Type { get; set; }

        public int ForumId { get; set; }
    }
}