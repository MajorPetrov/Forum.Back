using System.Collections.Generic;

namespace ForumJV.Data.Models
{
    public class Poll : BaseEntity
    {
        public string Question { get; set; }
        public IEnumerable<PollOption> Options { get; set; }

        public int PostId { get; set; }
        public Post Post { get; set; }
    }
}