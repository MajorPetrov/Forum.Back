using System;

namespace Forum.Models.Badge
{
    public class BadgeModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public DateTime ObtainingDate { get; set; }
    }
}