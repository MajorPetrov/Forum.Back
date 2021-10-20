using System.Collections.Generic;
using System.Threading.Tasks;
using Forum.Data.Models;

namespace Forum.Data.Services
{
    public interface IPostReply
    {
        Task<PostReply> GetById(int id);
        Task<IEnumerable<PostReply>> GetRepliesByIpAddress(string ipAddress);
        Task<IEnumerable<PostReply>> GetRepliesByPage(int postId, int pageNumber);
        Task<IEnumerable<PostReply>> GetPinnedRepliesByPost(int id);
        Task<int> GetRepliesCountByPost(int id);
        Task Create(PostReply reply);
        Task Edit(int id, string message);
        Task Delete(int id);
        Task Pin(int id);
    }
}