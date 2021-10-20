using System;

namespace Forum.Data.Models
{
    public class UserBadge
    {
        public string UserId { get; set; }
        public int BadgeId { get; set; }
        public DateTime ObtainingDate { get; set; }
    }
}