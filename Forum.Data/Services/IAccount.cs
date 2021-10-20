using System.Threading.Tasks;
using Forum.Data.Models;

namespace Forum.Data.Services
{
    public interface IAccount
    {
        Task<string> CreateToken(ApplicationUser user)
    }
}