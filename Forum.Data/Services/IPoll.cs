using System.Collections.Generic;
using System.Threading.Tasks;
using Forum.Data.Models;

namespace Forum.Data.Services
{
    public interface IPoll
    {
        Task<Poll> GetById(int id);
        Task<PollOption> GetOptionById(int id);
        // Task<IEnumerable<PollOption>> GetAllPollOptionsById(int id);
        // Task<IEnumerable<PollVote>> GetUserVotes(string userId);
        Task<int> GetVotesCountByOption(int optionId);
        Task<bool> HasUserVoted(int optionId, string userId);
        Task Create(Poll poll);
        Task Delete(int id);
        Task Vote(int optionId, string userId);
        // Task CancelVote(int optionId, string userId);
    }
}