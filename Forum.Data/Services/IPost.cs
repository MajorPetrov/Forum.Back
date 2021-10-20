using System.Collections.Generic;
using System.Threading.Tasks;
using ForumJV.Data.Models;

namespace ForumJV.Data.Services
{
    public interface IPost
    {
        Task<Post> GetById(int id);
        Task<IEnumerable<Post>> GetPinnedPosts();
        Task<IEnumerable<Post>> GetFilteredPosts(string searchMode, string searchQuery, int pageNumber);
        Task<IEnumerable<Post>> GetPostsByPage(int forumId, int pageNumber);
        Task<IEnumerable<Post>> GetPostsByIpAddress(string ipAddress);
        Task<ArchivedPost> GetArchivedPostById(int id);
        Task<FavoritePost> GetFavoritePostByIds(int postId, string userId);
        Task<FollowedPost> GetFollowedPostByIds(int postId, string userId);
        Task<bool> IsLocked(int id);
        Task<bool> IsFavorite(int postId, string userId);
        Task<bool> IsFollowed(int postId, string userId);
        Task Create(Post post);
        Task EditPostTitle(int id, string newTitle);
        Task EditPostContent(int id, string newContent);
        Task Delete(int id);
        Task Pin(int id);
        Task Lock(ArchivedPost archivedPost);
        Task Unlock(int id);
        Task Favorite(int postId, string userId);
        Task Unfavorite(int postId, string userId);
        Task Follow(int postId, string userId);
        Task Unfollow(int postId, string userId);
        Task UpdateLastReplyDate(int id);
    }
}