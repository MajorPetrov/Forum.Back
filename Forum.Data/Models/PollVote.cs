namespace ForumJV.Data.Models
{
    public class PollVote : BaseEntity
    {
        public string UserId { get; set; }
        public int OptionId { get; set; }
    }
}