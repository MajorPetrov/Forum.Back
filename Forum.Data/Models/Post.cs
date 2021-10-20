using System;
using System.Collections.Generic;

namespace Forum.Data.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public string IpAddress { get; set; }
        public Color Type { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastReplyDate { get; set; }
        public virtual ApplicationUser User { get; set; }
        public virtual Forum Forum { get; set; }
        public virtual Poll Poll { get; set; }
        public virtual IEnumerable<PostReply> Replies { get; set; }
    }
}