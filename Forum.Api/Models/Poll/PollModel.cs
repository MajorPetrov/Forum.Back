using System.Collections.Generic;

namespace ForumJV.Models.Poll
{
    public class PollModel
    {
        public string Question { get; set; }
        public int VotesCount { get; set; }
        public IEnumerable<PollOptionModel> Options { get; set; }
    }
}