using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ForumJV.Data;
using ForumJV.Data.Models;
using ForumJV.Data.Services;

namespace ForumJV.Services
{
    public class PollService : IPoll
    {
        private readonly ApplicationDbContext _context;

        public PollService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Poll> GetById(int id)
        {
            return await _context.Polls.Include(poll => poll.Options)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PollOption> GetOptionById(int id)
        {
            return await _context.PollOptions.Where(option => option.Id == id)
                .Include(option => option.Poll).FirstOrDefaultAsync();
        }

        public async Task<int> GetVotesCountByOption(int optionId)
        {
            return await _context.PollVotes.Where(vote => vote.OptionId == optionId).CountAsync();
        }

        public async Task<bool> HasUserVoted(int optionId, string userId)
        {
            return await _context.PollVotes.AnyAsync(vote => vote.OptionId == optionId && vote.UserId == userId);
        }

        public async Task Create(Poll poll)
        {
            await _context.AddAsync(poll); // Les options elles-mêmes sont ajoutées automatiquement dans la BDD (pratique)
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var poll = await GetById(id);

            _context.Remove(poll);
            _context.RemoveRange(poll.Options); // Pour la suppression par contre, c'est pas automatique (wtf)
            await _context.SaveChangesAsync();
        }

        public async Task Vote(int optionId, string userId)
        {
            await _context.AddAsync(new PollVote
            {
                UserId = userId,
                OptionId = optionId
            });
            await _context.SaveChangesAsync();
        }
    }
}