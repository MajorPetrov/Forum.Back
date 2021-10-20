using System;

namespace ForumJV.Data.Models
{
    public class PostReply : BaseEntity
    {
        public string Content { get; set; }
        public bool IsPinned { get; set; }
        public string IpAddress { get; set; }
        public DateTime Created { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int PostId { get; set; }
        public Post Post { get; set; }
    }
}