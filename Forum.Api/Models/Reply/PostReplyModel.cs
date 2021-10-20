using System;
using Forum.Data;

namespace Forum.Models.Reply
{
    public class PostReplyModel
    {
        public int Id { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorImageUrl { get; set; }
        public int AuthorCancer { get; set; }
        public string AuthorRole { get; set; }
        public string Content { get; set; }
        public bool IsPinned { get; set; }
        public string AuthorSignature { get; set; }
        public int PostId { get; set; }
        public Color PostType { get; set; }
        public DateTime Created { get; set; }
    }
}