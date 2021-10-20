namespace ForumJV.Data.Models
{
    public class PollOption : BaseEntity
    {
        public string Answer { get; set; }
        
        public int PollId { get; set; }
        public Poll Poll { get; set; }
    }
}