using System.Collections.Generic;

namespace ForumJV.Models.Notification
{
    public class NotificationListingModel
    {
        public IEnumerable<NotificationModel> Notifications { get; set; }
    }
}