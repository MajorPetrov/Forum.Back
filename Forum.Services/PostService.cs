using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ForumJV.Data;
using ForumJV.Data.Models;
using ForumJV.Data.Services;

namespace ForumJV.Services
{
    public class PostService : IPost
    {
        private readonly ApplicationDbContext _context;
        const int postsPerPage = 25;

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

        public async Task<IEnumerable<Post>> GetPinnedPosts()
        {
            return await _context.Posts.Where(post => post.IsPinned)
                .Include(post => post.User)
                .Include(post => post.Poll)
                .OrderByDescending(post => post.LastReplyDate).ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetFilteredPosts(string searchMode, string searchQuery, int pageNumber)
        {
            var startPosition = (pageNumber - 1) * postsPerPage;
            var posts = await _context.Posts.Include(post => post.User)
                .Include(post => post.Forum).ToListAsync();

            if (searchMode.Equals("Author"))
            {
                return await _context.Posts
                    .Include(post => post.User)
                    .Include(post => post.Forum)
                    .Where(post => post.User.NormalizedUserName.Contains(searchQuery.ToUpperInvariant()))
                        .OrderByDescending(post => post.LastReplyDate).Skip(startPosition).Take(postsPerPage)
                            .ToListAsync();
            }
            // else if (searchMode.Equals("Content"))
            // {
            //     var normalized = searchQuery.Normalize().ToLowerInvariant();

            //     return posts.Where(post => post.Content.ToLower().Contains(normalized))
            //         .OrderByDescending(post => post.LastReplyDate).Skip(startPosition).Take(postsPerPage);
            // }
            else
            {
                return await _context.Posts
                    .Include(post => post.User)
                    .Include(post => post.Forum)
                    .Where(post => post.Title.ToLower().Contains(searchQuery.ToLowerInvariant()))
                        .OrderByDescending(post => post.LastReplyDate).Skip(startPosition).Take(postsPerPage)
                            .ToListAsync();
            }
        }

        public async Task<IEnumerable<Post>> GetPostsByPage(int forumId, int pageNumber)
        {
            var startPosition = (pageNumber - 1) * postsPerPage;

            return await _context.Posts.Where(post => post.Forum.Id == forumId && !post.IsPinned)
                .Include(post => post.User)
                .Include(post => post.Poll)
                .OrderByDescending(post => post.LastReplyDate)
                .Skip(startPosition).Take(postsPerPage).ToListAsync();
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

        public async Task<FavoritePost> GetFavoritePostByIds(int postId, string userId)
        {
            return await _context.FavoritePosts.FirstOrDefaultAsync(post => post.PostId == postId && post.UserId == userId);
        }

        public async Task<FollowedPost> GetFollowedPostByIds(int postId, string userId)
        {
            return await _context.FollowedPost.FirstOrDefaultAsync(post => post.PostId == postId && post.UserId == userId);
        }

        public async Task<bool> IsLocked(int id)
        {
            return await _context.ArchivedPosts.AnyAsync(arch => arch.PostId == id);
        }

        public async Task<bool> IsFavorite(int postId, string userId)
        {
            return await _context.FavoritePosts.AnyAsync(fav => fav.PostId == postId && fav.UserId == userId);
        }

        public async Task<bool> IsFollowed(int postId, string userId)
        {
            return await _context.FollowedPost.AnyAsync(fol => fol.PostId == postId && fol.UserId == userId);
        }

        public async Task Create(Post post)
        {
            // var i = 1;
            // foreach (var item in _context.Polls)
            // {
            //     item.Id = i;
            //     i++;
            // }

            // à supprimer lors du passage au Guid
            // la colonne p1.PollId n'existe pas
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

            if (await IsLocked(id))
                await Unlock(id);

            if (post.Poll != null)
                _context.Polls.Remove(post.Poll);

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

        public async Task Favorite(int postId, string userId)
        {
            _context.Add(new FavoritePost
            {
                PostId = postId,
                UserId = userId
            });
            await _context.SaveChangesAsync();
        }

        public async Task Unfavorite(int postId, string userId)
        {
            var favoritePost = GetFavoritePostByIds(postId, userId);

            _context.Remove(favoritePost);
            await _context.SaveChangesAsync();
        }

        public async Task Follow(int postId, string userId)
        {
            _context.Add(new FollowedPost
            {
                PostId = postId,
                UserId = userId
            });
            await _context.SaveChangesAsync();
        }

        public async Task Unfollow(int postId, string userId)
        {
            var followedPost = GetFollowedPostByIds(postId, userId);

            _context.Remove(followedPost);
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