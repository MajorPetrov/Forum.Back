using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ForumJV.Data.Models;
using ForumJV.Data.Services;
using ForumJV.Models.Post;
using ForumJV.Models.Search;

namespace ForumJV.Controllers
{
    [Route("api/[controller]")]
    public class SearchController : Controller
    {
        private readonly IPost _postService;
        private readonly IPostReply _replyService;
        private readonly UserManager<ApplicationUser> _userManager;

        public SearchController(IPost postService, IPostReply replyService, UserManager<ApplicationUser> userManager)
        {
            _postService = postService;
            _replyService = replyService;
            _userManager = userManager;
        }

        /// <summary>
        /// Renvoie une liste de sujets filtrée par le paramètre
        /// </summary>
        /// <param name="searchMode">Chaîne de caractères représentant ce qu'il faut chercher (par défaut, cherche par titre et contenu)</param>
        /// <param name="searchQuery">Chaîne de caractères devant être présente dans le titre ou le contenu du sujet</param>
        /// <param name="pageNumber">Numéro de la page à afficher</param>
        /// <returns>Une vue contenant les sujets dont le nom ou le contenu contient la chaîne de caractères passée en paramètre</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Results(string searchMode, string searchQuery, int pageNumber = 1)
        {
            var posts = await _postService.GetFilteredPosts(searchMode, searchQuery, pageNumber);
            var noResults = (!string.IsNullOrEmpty(searchQuery) && !posts.Any());
            var postListings = new List<PostListingModel>();

            foreach (var post in posts)
                postListings.Add(await BuildPostListing(post));

            var model = new SearchResultModel
            {
                SearchQuery = searchQuery,
                EmptySearchQuery = noResults,
                Posts = postListings
            };

            return Json(model);
        }

        /// <summary>
        /// Renvoie le résultat de la méthode Results avec comme paramètre la chaîne de caractère passée en paramètre.
        /// </summary>
        /// <param name="searchMode">Chaîne de caractères représentant ce qu'il faut chercher (par défaut, cherche par titre et contenu)</param>
        /// <param name="searchQuery">Chaîne de caractère devant être présente dans le titre ou le contenu du sujet</param>
        /// <returns>le résultat de la méthode Results</returns>
        [HttpPost("[action]")]
        public IActionResult Search(string searchMode, string searchQuery)
        {
            return RedirectToAction("Results", new { searchMode, searchQuery });
        }

        private async Task<PostListingModel> BuildPostListing(Post post)
        {
            var userRoles = await _userManager.GetRolesAsync(post.User);
            var repliesCount = await _replyService.GetRepliesCountByPost(post.Id).ConfigureAwait(false);

            return new PostListingModel
            {
                Id = post.Id,
                Title = post.Title,
                AuthorId = post.UserId,
                AuthorName = post.User.UserName,
                AuthorRole = userRoles.FirstOrDefault(),
                LastReplyDate = post.LastReplyDate,
                RepliesCount = repliesCount,
                IsPinned = post.IsPinned,
                IsLocked = post.IsLocked,
                HasPoll = post.Poll == null ? false : true,
                Type = post.Type,
            };
        }
    }
}