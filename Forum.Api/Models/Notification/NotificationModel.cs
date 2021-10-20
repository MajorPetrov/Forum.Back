using System;

namespace ForumJV.Models.Notification
{
    public class NotificationModel
    {
        public int Id { get; set; }
        public string MentionedUserId { get; set; }
        public string MentionedUserName { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public int ContentId { get; set; }
        public string TitleContent { get; set; }
        public int PageNumber { get; set; }
        public DateTime Created { get; set; }
    }
}