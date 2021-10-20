using System;

namespace ForumJV.Data.Models
{
    public class Notification : BaseEntity
    {
        public DateTime Created { get; set; }
        public int? PostId { get; set; }
        public int? ReplyId { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string MentionedUserId { get; set; }
        public ApplicationUser MentionedUser { get; set; }
    }
}