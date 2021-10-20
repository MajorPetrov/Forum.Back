using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ForumJV.Data.Models
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        // [DefaultValue(false)]
        // public bool Deleted { get; set; }
    }
}