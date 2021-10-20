using System;
using Microsoft.AspNetCore.Identity;

namespace ForumJV.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string ProfileImageUrl { get; set; }
        public int Cancer { get; set; }
        public DateTime MemberSince { get; set; }
        public string Signature { get; set; }
        public string IpAddress { get; set; }
    }
}
