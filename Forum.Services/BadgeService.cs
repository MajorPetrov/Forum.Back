using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ForumJV.Data;
using ForumJV.Data.Models;
using ForumJV.Data.Services;

namespace ForumJV.Services
{
    public class BadgeService : IBadge
    {
        private readonly ApplicationDbContext _context;

        public BadgeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Badge> GetById(int id)
        {
            return await _context.Badges.FirstOrDefaultAsync(badge => badge.Id == id);
        }

        public async Task<UserBadge> GetUserBadgeByIds(string userId, int badgeId)
        {
            return await _context.UserBadges.FirstOrDefaultAsync(userBadge => userBadge.UserId == userId && userBadge.BadgeId == badgeId);
        }

        public async Task<IEnumerable<Badge>> GetAll()
        {
            return await _context.Badges.ToListAsync();
        }

        public async Task<IEnumerable<UserBadge>> GetUserBadges(string userId)
        {
            return await _context.UserBadges.Where(userBadge => userBadge.UserId == userId).ToListAsync();
        }

        public async Task Create(Badge badge)
        {
            await _context.AddAsync(badge);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var badge = await GetById(id);

            _context.Remove(badge);
            await _context.SaveChangesAsync();
        }

        public async Task AssignBadgeToUser(string userId, int badgeId)
        {
            await _context.AddAsync(new UserBadge
            {
                UserId = userId,
                BadgeId = badgeId,
                ObtainingDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task RemoveBadgeFromUser(string userId, int badgeId)
        {
            var userBadge = await GetUserBadgeByIds(userId, badgeId);

            _context.Remove(userBadge);
            await _context.SaveChangesAsync();
        }
    }
}