using System.Collections.Generic;
using Forum.Models.Post;

namespace Forum.Models.Forum
{
    public class ForumTopicModel
    {
        public string SearchQuery { get; set; }
        public ForumListingModel Forum { get; set; }
        public IEnumerable<PostListingModel> Posts { get; set; }
    }
}