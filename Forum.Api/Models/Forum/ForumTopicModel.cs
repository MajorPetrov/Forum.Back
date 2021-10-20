using System.Collections.Generic;
using ForumJV.Models.Post;

namespace ForumJV.Models.Forum
{
    public class ForumTopicModel
    {
        public string SearchQuery { get; set; }
        public ForumListingModel Forum { get; set; }
        public IEnumerable<PostListingModel> Posts { get; set; }
    }
}