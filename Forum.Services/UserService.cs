using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ForumJV.Data;
using ForumJV.Data.Models;
using ForumJV.Data.Services;

namespace ForumJV.Services
{
    public class UserService : IApplicationUser
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ApplicationUser> GetById(string id)
        {
            return await _context.ApplicationUsers.FirstOrDefaultAsync(user => user.Id == id);
        }

        public async Task<IEnumerable<ApplicationUser>> GetByIpAddress(string ipAddress)
        {
            return await _context.ApplicationUsers.Where(user => user.IpAddress.Equals(ipAddress)).ToListAsync();
        }

        public async Task SetProfileImageAsync(string id, string path)
        {
            var user = await GetById(id);
            user.ProfileImageUrl = path;

            _context.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSignature(string id, string newSignature)
        {
            var user = await GetById(id);
            user.Signature = newSignature;

            _context.Update(user);
            await _context.SaveChangesAsync();
        }

        // public async Task IncrementCancer(string id)
        // {
        //     var user = await GetById(id);
        //     user.Cancer += 1;

        //     _context.Update(user);
        //     await _context.SaveChangesAsync();
        // }

        public async Task Unban(string id)
        {
            var user = await GetById(id);
            user.LockoutEnd = null;

            _context.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task ChangeRole(string id, string role)
        {
            var user = await GetById(id);
            var roles = await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(user, roles.ToArray());
            await _userManager.AddToRoleAsync(user, role);
            await _userManager.UpdateAsync(user);
        }
    }
}