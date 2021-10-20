using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Forum.Data;
using Forum.Data.Models;
using Forum.Data.Services;

namespace Forum.Services
{
    public class PostReplyService : IPostReply
    {
        private readonly ApplicationDbContext _context;
        const int repliesPerPage = 20;

        public PostReplyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PostReply> GetById(int id)
        {
            return await _context.PostReplies.Where(reply => reply.Id == id)
            .Include(reply => reply.User)
            .Include(reply => reply.Post)
                .ThenInclude(post => post.User).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PostReply>> GetRepliesByIpAddress(string ipAddress)
        {
            return await _context.PostReplies.Where(reply => reply.IpAddress.Equals(ipAddress))
                .Include(reply => reply.User).ToListAsync();
        }

        public async Task<IEnumerable<PostReply>> GetRepliesByPage(int postId, int pageNumber)
        {
            var startPosition = (pageNumber == 1) ? 0 : (pageNumber - 1) * repliesPerPage - 1;
            var toTake = (pageNumber == 1) ? repliesPerPage - 1 : repliesPerPage;

            return await _context.PostReplies.Where(reply => reply.Post.Id == postId)
                .Include(reply => reply.User)
                .Include(reply => reply.Post)
                .OrderBy(reply => reply.Created).Skip(startPosition).Take(toTake).ToListAsync();
        }

        public async Task<IEnumerable<PostReply>> GetPinnedRepliesByPost(int postId)
        {
            return await _context.PostReplies.Where(reply => reply.Post.Id == postId && reply.IsPinned)
                .Include(reply => reply.User)
                .Include(reply => reply.Post)
                .OrderBy(reply => reply.Created).ToListAsync();
        }

        public async Task<int> GetRepliesCountByPost(int id)
        {
            return await _context.PostReplies.CountAsync(reply => reply.Post.Id == id);
        }

        public async Task Create(PostReply reply)
        {
            // à supprimer lors du passage au Guid
            var lastReply = await _context.PostReplies.OrderBy(r => r.Id).LastAsync();
            reply.Id = lastReply.Id + 1;

            await _context.PostReplies.AddAsync(reply);
            await _context.SaveChangesAsync();
        }

        public async Task Edit(int id, string message)
        {
            var reply = await GetById(id);
            reply.Content = message;

            _context.Update(reply);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var reply = await GetById(id);

            _context.Remove(reply);
            await _context.SaveChangesAsync();
        }

        public async Task Pin(int id)
        {
            var reply = await GetById(id);
            reply.IsPinned = !reply.IsPinned;

            await _context.SaveChangesAsync();
        }
    }
}