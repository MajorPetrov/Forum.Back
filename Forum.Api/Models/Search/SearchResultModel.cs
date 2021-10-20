using System.Collections.Generic;
using Forum.Models.Post;

namespace Forum.Models.Search
{
    public class SearchResultModel
    {
        public string SearchQuery { get; set; }
        public bool EmptySearchQuery { get; set; }
        public IEnumerable<PostListingModel> Posts { get; set; }
    }
}