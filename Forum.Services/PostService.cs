using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Forum.Data;
using Forum.Data.Models;
using Forum.Data.Services;

namespace Forum.Services
{
    public class PostService : IPost
    {
        private readonly ApplicationDbContext _context;

        public PostService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Post> GetById(int id)
        {
            return await _context.Posts.Where(post => post.Id == id)
                .Include(post => post.User)
                .Include(post => post.Poll)
                    .ThenInclude(poll => poll.Options)
                .Include(post => post.Forum)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Post>> GetPinnedPosts(int forumId)
        {
            return await _context.Posts.Where(post => post.IsPinned && post.Forum.Id == forumId)
                .Include(post => post.User)
                .Include(post => post.Poll)
                .OrderByDescending(post => post.LastReplyDate).ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetFilteredPosts(string searchMode, string searchQuery, int pageNumber)
        {
            var startPosition = (pageNumber - 1) * 25;
            var posts = await _context.Posts.Include(post => post.User)
                .Include(post => post.Forum).ToListAsync();

            if (searchMode.Equals("Author"))
            {
                var normalized = searchQuery.Normalize().ToUpperInvariant();

                return posts.Where(post => post.User.NormalizedUserName.Contains(normalized))
                    .OrderByDescending(post => post.LastReplyDate).Skip(startPosition).Take(25);
            }
            // else if (searchMode.Equals("Content"))
            // {
            //     var normalized = searchQuery.Normalize().ToLowerInvariant();

            //     return posts.Where(post => post.Content.ToLower().Contains(normalized))
            //         .OrderByDescending(post => post.LastReplyDate).Skip(startPosition).Take(25);
            // }
            else
            {
                var normalized = searchQuery.Normalize().ToLowerInvariant();

                return posts.Where(post => post.Title.ToLower().Contains(normalized))
                    .OrderByDescending(post => post.LastReplyDate).Skip(startPosition).Take(25);
            }
        }

        public async Task<IEnumerable<Post>> GetPostsByPage(int forumId, int pageNumber)
        {
            var startPosition = (pageNumber - 1) * 25;

            return await _context.Posts.Where(post => post.Forum.Id == forumId && !post.IsPinned)
                .Include(post => post.User)
                .Include(post => post.Poll)
                .OrderByDescending(post => post.LastReplyDate)
                .Skip(startPosition).Take(25).ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsByIpAddress(string ipAddress)
        {
            return await _context.Posts.Where(post => post.IpAddress.Equals(ipAddress))
                .Include(post => post.User).ToListAsync();
        }

        public async Task<ArchivedPost> GetArchivedPostById(int id)
        {
            return await _context.ArchivedPosts.FirstOrDefaultAsync(post => post.PostId == id);
        }

        public async Task<bool> IsLock(int id)
        {
            return await _context.ArchivedPosts.AnyAsync(arch => arch.PostId == id);
        }

        public async Task Create(Post post)
        {
            // à supprimer lors du passage au Guid
            var lastPost = await _context.Posts.OrderBy(p => p.Id).LastAsync();
            post.Id += lastPost.Id + 1;

            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();
        }

        public async Task EditPostTitle(int id, string newTitle)
        {
            var post = await GetById(id);
            post.Title = newTitle;

            await _context.SaveChangesAsync();
        }

        public async Task EditPostContent(int id, string newContent)
        {
            var post = await GetById(id);
            post.Content = newContent;

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var post = await GetById(id);

            if (await IsLock(id))
                await Unlock(id);

            _context.Remove(post);
            await _context.SaveChangesAsync();
        }

        public async Task Lock(ArchivedPost archivedPost)
        {
            var post = await GetById(archivedPost.PostId);
            post.IsLocked = true;

            await _context.AddAsync(archivedPost);
            await _context.SaveChangesAsync();
        }

        public async Task Unlock(int id)
        {
            var archivedPost = await GetArchivedPostById(id);
            var post = await GetById(id);
            post.IsLocked = false;

            _context.Remove(archivedPost);
            await _context.SaveChangesAsync();
        }

        public async Task Pin(int id)
        {
            var post = await GetById(id);
            post.IsPinned = !post.IsPinned;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateLastReplyDate(int id)
        {
            var post = await _context.Posts.Where(post => post.Id == id)
                .Include(post => post.Replies).FirstOrDefaultAsync();

            var lastReply = post.Replies.OrderByDescending(rep => rep.Created).FirstOrDefault();

            if (lastReply == null)
                post.LastReplyDate = post.Created;
            else
                post.LastReplyDate = lastReply.Created;

            await _context.SaveChangesAsync();
        }
    }
}