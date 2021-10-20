using System.Collections.Generic;

namespace Forum.Data.Models
{
    public class Poll
    {
        public int PostId { get; set; }
        public string Question { get; set; }
        public virtual IEnumerable<PollOption> Options { get; set; }
    }
}