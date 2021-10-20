using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ForumJV.Data;
using ForumJV.Data.Models;
using ForumJV.Data.Services;

namespace ForumJV.Services
{
    public class NotificationService : INotification
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> GetById(int id)
        {
            return await _context.Notifications.Where(notif => notif.Id == id)
                .Include(notif => notif.User)
                .Include(notif => notif.MentionedUser)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Notification>> GetByPostId(int id)
        {
            return await _context.Notifications.Where(notif => notif.PostId == id).ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetByReplyId(int id)
        {
            return await _context.Notifications.Where(notif => notif.ReplyId == id).ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUserNotifications(string userId)
        {
            return await _context.Notifications.Where(notif => notif.User.Id == userId)
                .ToListAsync();
        }

        public async Task Create(IEnumerable<Notification> notifications)
        {
            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var notification = await GetById(id);

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMany(IEnumerable<Notification> notifications)
        {
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAll(string userId)
        {
            var notifications = await GetUserNotifications(userId);

            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();
        }
    }
}