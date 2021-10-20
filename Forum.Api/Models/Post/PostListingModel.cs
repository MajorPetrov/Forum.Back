using System;
using Forum.Data;
using Forum.Models.Forum;

namespace Forum.Models.Post
{
    public class PostListingModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorRole { get; set; }
        public int AuthorCancer { get; set; }
        public int RepliesCount { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public bool HasPoll { get; set; }
        public Color Type { get; set; }
        public DateTime LastReplyDate { get; set; }
    }
}