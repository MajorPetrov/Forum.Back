using System.Collections.Generic;
using System.Threading.Tasks;
using ForumJV.Data.Models;

namespace ForumJV.Data.Services
{
    public interface IForum
    {
        Task<Forum> GetById(int id);
        // Task<IEnumerable<Forum>> GetAll();
        Task<int> Count();
        // Task Create(Forum forum);
        // Task Delete(int id);
        // Task UpdateForumTitle(int id, string newTitle);
        // Task UpdateForumDescription(int id, string newDescription);
    }
}
