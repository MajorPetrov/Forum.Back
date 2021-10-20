using System.Collections.Generic;
using ForumJV.Models.Post;

namespace ForumJV.Models.Search
{
    public class SearchResultModel
    {
        public string SearchQuery { get; set; }
        public bool EmptySearchQuery { get; set; }
        public IEnumerable<PostListingModel> Posts { get; set; }
    }
}