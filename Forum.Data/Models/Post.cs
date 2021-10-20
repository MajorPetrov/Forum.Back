using System;
using System.Collections.Generic;

namespace ForumJV.Data.Models
{
    public class Post : BaseEntity
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public string IpAddress { get; set; }
        public Color Type { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastReplyDate { get; set; }
        public IEnumerable<PostReply> Replies { get; set; }
        public Poll Poll { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int ForumId { get; set; }
        public Forum Forum { get; set; }

    }
}