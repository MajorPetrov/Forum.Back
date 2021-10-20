using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ForumJV.Data.Models;
using ForumJV.Data.Services;
using ForumJV.Models;
using ForumJV.Models.Home;
using ForumJV.Models.Post;

namespace ForumJV.Controllers
{
    [Route("api/[controller]")]
    public class HomeController : Controller
    {
        private readonly IPost _postService;
        private readonly IPostReply _replyService;

        public HomeController(IPost postService, IPostReply replyService)
        {
            _postService = postService;
            _replyService = replyService;
        }

        /// <summary>
        /// Renvoie une vue contenant les 25 derniers sujets créés
        /// </summary>
        /// <returns>Une vue contenant les 25 derniers sujets créés</returns>
        [HttpGet("[action]")]
        public IActionResult Index()
        {
            var model = BuildHomeIndexModel();

            return Json(model);
        }

        /// <summary>
        /// Renvoie la vue associée à page "About".
        /// </summary>
        /// <returns>Renvoie la page "/Home/About.cshtml"</returns>
        [HttpGet("[action]")]
        public IActionResult About()
        {
            ViewData["Message"] = "Où ai-je atterri ?";

            return Ok();
        }

        /// <summary>
        /// Renvoie la vue associée à page "Privacy".
        /// </summary>
        /// <returns>Renvoie la page "/Home/Privacy.cshtml"</returns>
        [HttpGet("[action]")]
        public IActionResult Privacy()
        {
            return Ok();
        }

        /// <summary>
        /// Renvoie la vue associé à la page d'erreur par défaut du framework.
        /// </summary>
        /// <returns>Revoie une page d'erreur contenant les information d'erreur (identifiant de la requête et le contexte HTTP)</returns>
        [HttpGet("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return BadRequest(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<HomeIndexModel> BuildHomeIndexModel()
        {
            var latestPosts = await _postService.GetPostsByPage(1, 1);
            var gettedPosts = latestPosts.OrderByDescending(post => post.IsPinned);
            var posts = new List<PostListingModel>();

            foreach (var post in latestPosts)
                posts.Add(await BuildPostListing(post));

            return new HomeIndexModel()
            {
                SearchQuery = "",
                LatestPosts = posts
            };
        }

        private async Task<PostListingModel> BuildPostListing(Post post)
        {
            return new PostListingModel
            {
                Id = post.Id,
                Title = post.Title,
                AuthorId = post.UserId,
                AuthorName = post.User.UserName,
                AuthorCancer = post.User.Cancer,
                LastReplyDate = post.LastReplyDate,
                RepliesCount = await _replyService.GetRepliesCountByPost(post.Id),
                IsPinned = post.IsPinned,
                IsLocked = post.IsLocked,
                HasPoll = post.Poll == null ? false : true,
                Type = post.Type
            };
        }
    }
}
