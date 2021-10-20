using System;
using System.Collections.Generic;
using Forum.Models.Badge;

namespace Forum.Models.ApplicationUser
{
    public class ProfileModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int Cancer { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Signature { get; set; }
        public bool IsBanned { get; set; }
        public string Role { get; set; }
        public DateTime MemberSince { get; set; }
        public IEnumerable<BadgeModel> Badges { get; set; }
    }
}