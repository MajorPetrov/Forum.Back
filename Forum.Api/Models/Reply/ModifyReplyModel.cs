using System.ComponentModel.DataAnnotations;

namespace Forum.Models.Reply
{
    public class ModifyReplyModel
    {
        public int Id { get; set; }

        [StringLength(65536, ErrorMessage = "Le {0} doit comporter au moins {2} et au maximum {1} caractères.", MinimumLength = 3)]
        public string Content { get; set; }
    }
}