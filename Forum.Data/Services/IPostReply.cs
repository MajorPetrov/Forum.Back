using System.Collections.Generic;
using System.Threading.Tasks;
using ForumJV.Data.Models;

namespace ForumJV.Data.Services
{
    public interface IPostReply
    {
        Task<PostReply> GetById(int id);
        Task<IEnumerable<PostReply>> GetRepliesByIpAddress(string ipAddress);
        Task<IEnumerable<PostReply>> GetRepliesByPage(int postId, int pageNumber);
        Task<IEnumerable<PostReply>> GetPinnedRepliesByPost(int id);
        Task<int> GetRepliesCountByPost(int id);
        Task<int> GetReplyPage(PostReply reply);
        Task Create(PostReply reply);
        Task Edit(int id, string message);
        Task Delete(int id);
        Task Pin(int id);
    }
}