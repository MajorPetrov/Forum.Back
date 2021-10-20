using System.Collections.Generic;
using System.Threading.Tasks;
using Forum.Data.Models;

namespace Forum.Data.Services
{
    public interface IBadge
    {
        Task<Badge> GetById(int id);
        Task<UserBadge> GetUserBadgeByIds(string userId, int badgeId);
        Task<IEnumerable<Badge>> GetAll();
        Task<IEnumerable<UserBadge>> GetUserBadges(string userId);
        Task Create(Badge badge);
        Task Delete(int id);
        Task AssignBadgeToUser(string userId, int badgeId);
        Task RemoveBadgeFromUser(string userId, int badgeId);
    }
}