using System.Collections.Generic;

namespace Forum.Models.Poll
{
    public class PollModel
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public int VotesCount { get; set; }
        public IEnumerable<PollOptionModel> Options { get; set; }
    }
}