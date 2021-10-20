using System.Collections.Generic;
using System.Threading.Tasks;
using ForumJV.Data.Models;

namespace ForumJV.Data.Services
{
    public interface INotification
    {
        Task<Notification> GetById(int id);
        Task<IEnumerable<Notification>> GetByPostId(int id);
        Task<IEnumerable<Notification>> GetByReplyId(int id);
        Task<IEnumerable<Notification>> GetUserNotifications(string userId);
        Task Create(IEnumerable<Notification> notifications);
        Task Delete(int id);
        Task DeleteMany(IEnumerable<Notification> notifications);
        Task DeleteAll(string userId);
    }
}