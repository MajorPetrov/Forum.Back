namespace Forum.Models.Poll
{
    public class PollOptionModel
    {
        public int Id { get; set; }
        public string Answer { get; set; }
        public int VotesCount { get; set; }
        public bool Selected { get; set; }
    }
}