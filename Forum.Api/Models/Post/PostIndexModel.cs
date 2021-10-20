using System;
using System.Collections.Generic;
using Forum.Data;
using Forum.Models.Poll;
using Forum.Models.Reply;

namespace Forum.Models.Post
{
    public class PostIndexModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorImageUrl { get; set; }
        public int AuthorCancer { get; set; }
        public string AuthorSignature { get; set; }
        public string Content { get; set; }
        public int RepliesCount { get; set; }
        public Color Type { get; set; }
        public string AuthorRole { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public string LockReason { get; set; }
        public int ForumId { get; set; }
        public string ForumName { get; set; }
        public DateTime Created { get; set; }
        public PollModel Poll { get; set; }
        public IEnumerable<PostReplyModel> Replies { get; set; }
    }
}