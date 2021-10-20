using System.Collections.Generic;
using ForumJV.Models.Post;

namespace ForumJV.Models.Home
{
    public class HomeIndexModel
    {
        public string SearchQuery { get; set; }
        public IEnumerable<PostListingModel> LatestPosts { get; set; }
    }
}