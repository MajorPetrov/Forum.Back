using System;

namespace ForumJV.Data.Models
{
    public class UserBadge
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int BadgeId { get; set; }
        public DateTime ObtainingDate { get; set; }
    }
}