using System.Collections.Generic;
using System.Threading.Tasks;
using Forum.Data.Models;

namespace Forum.Data.Services
{
    public interface IPost
    {
        Task<Post> GetById(int id);
        Task<IEnumerable<Post>> GetPinnedPosts(int forumId);
        Task<IEnumerable<Post>> GetFilteredPosts(string searchMode, string searchQuery, int pageNumber);
        Task<IEnumerable<Post>> GetPostsByPage(int forumId, int pageNumber);
        Task<IEnumerable<Post>> GetPostsByIpAddress(string ipAddress);
        Task<ArchivedPost> GetArchivedPostById(int id);
        Task<bool> IsLock(int id);
        Task Create(Post post);
        Task EditPostTitle(int id, string newTitle);
        Task EditPostContent(int id, string newContent);
        Task Delete(int id);
        Task Pin(int id);
        Task Lock(ArchivedPost archivedPost);
        Task Unlock(int id);
        Task UpdateLastReplyDate(int id);
    }
}