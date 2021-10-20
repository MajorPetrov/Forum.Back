using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ForumJV.Models.Poll
{
    public class NewPollModel
    {
        public string Question { get; set; }
        public IList<string> Options { get; set; }
    }
}