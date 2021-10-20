using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Forum.Data;
using Forum.Data.Models;
using Forum.Data.Services;

namespace Forum.Services
{
    public class ForumService : IForum
    {
        private readonly ApplicationDbContext _context;
        private readonly IApplicationUser _userService;

        public ForumService(ApplicationDbContext context, IApplicationUser userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<Forum> GetById(int id)
        {
            return await _context.Forums.Where(fofo => fofo.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> Count()
        {
            return await _context.Forums.CountAsync();
        }
    }
}