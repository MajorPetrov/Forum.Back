namespace Forum.Data.Models
{
    public class PollOption
    {
        public int Id { get; set; }
        public string Answer { get; set; }
        public virtual Poll Poll { get; set; }
    }
}