using System.Collections.Generic;
using System.Threading.Tasks;
using Forum.Data.Models;

namespace Forum.Data.Services
{
    public interface IApplicationUser
    {
        Task<ApplicationUser> GetById(string id);
        Task<IEnumerable<ApplicationUser>> GetByIpAddress(string ipAddress);
        Task SetProfileImageAsync(string id, string path);
        Task UpdateSignature(string id, string newSignature);
        // Task IncrementCancer(string id);
        Task Unban(string id);
        Task ChangeRole(string id, string role);
    }
}