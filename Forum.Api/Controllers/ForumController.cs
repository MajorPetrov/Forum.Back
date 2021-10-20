using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Forum.Data;
using Forum.Data.Models;
using Forum.Data.Services;
using Forum.Models.Forum;
using Forum.Models.Post;

namespace Forum.Controllers
{
    [Route("api/[controller]")]
    public class ForumController : Controller
    {
        private readonly IForum _forumService;
        private readonly IPost _postService;
        private readonly IPostReply _replyService;
        private readonly UserManager<ApplicationUser> _userManager;
        private IMemoryCache _cache;

        public ForumController(IForum forumService, IPost postService, IPostReply replyService,
            UserManager<ApplicationUser> userManager, IMemoryCache cache)
        {
            _forumService = forumService;
            _postService = postService;
            _replyService = replyService;
            _userManager = userManager;
            _cache = cache;
        }

        /// <summary>
        /// Renvoie le sujet dont l'identifiant et dont le titre ou le contenu contenant la chaîne de caractères sont passés en paramètres.
        /// </summary>
        /// <param name="id">Identifiant du forum</param>
        /// <param name="pageNumber">Numéro de la page à afficher</param>
        /// <returns>Une vue sur la liste des sujets correspondant à l'identifiant du forum</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Topic(int id, int pageNumber = 1)
        {
            Forum forum;
            IEnumerable<Post> posts;
            IEnumerable<Post> pinnedPosts = new List<Post>();

            if (!User.Identity.IsAuthenticated)
            {
                if (!_cache.TryGetValue(CacheKeys.Forum + pageNumber, out forum))
                {
                    forum = await _forumService.GetById(id);

                    if (forum == null)
                        return NotFound(new { error = $"Le forum d'identifiant : '{id}' n'existe pas." });

                    _cache.Set(CacheKeys.Forum + id + pageNumber, forum, CacheKeys.Expiration);
                }

                if (!_cache.TryGetValue(CacheKeys.PostsByPage + pageNumber, out posts))
                {
                    posts = await _postService.GetPostsByPage(id, pageNumber);

                    _cache.Set(CacheKeys.PostsByPage + id + pageNumber, posts, CacheKeys.Expiration);
                }

                if (!_cache.TryGetValue(CacheKeys.PinnedPosts, out pinnedPosts))
                {
                    pinnedPosts = await _postService.GetPinnedPosts(id);

                    _cache.Set(CacheKeys.PinnedPosts + id + pageNumber, pinnedPosts, CacheKeys.Expiration);
                }
            }
            else
            {
                forum = await _forumService.GetById(id);
                posts = await _postService.GetPostsByPage(id, pageNumber);
                pinnedPosts = await _postService.GetPinnedPosts(id);
            }

            if (pageNumber == 1)
                posts = pinnedPosts.Concat(posts);

            var postListings = new List<PostListingModel>();

            foreach (var post in posts)
                postListings.Add(await BuildPostListing(post));

            var model = new ForumTopicModel
            {
                Posts = postListings,
                Forum = BuildForumListing(forum),
            };

            return Json(model);
        }

        /// <summary>
        /// Renvoie le résultat de la méthode Topic avec comme paramètre l'identifiant et la chaîne de caractères spécifiés.
        /// </summary>
        /// <param name="id">Identifiant du sujet</param>
        /// <param name="searchQuery">Chaîne de caractères devant être présente dans le titre ou le contenu du sujet.</param>
        /// <returns></returns>
        [HttpPost("[action]")]
        public IActionResult Search(int id, string searchQuery)
        {
            return RedirectToAction(nameof(Topic), new { id, searchQuery });
        }

        private async Task<PostListingModel> BuildPostListing(Post post)
        {
            var userRoles = await _userManager.GetRolesAsync(post.User);
            var repliesCount = await _replyService.GetRepliesCountByPost(post.Id).ConfigureAwait(false);

            return new PostListingModel
            {
                Id = post.Id,
                Title = post.Title,
                AuthorId = post.User.Id,
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

        private ForumListingModel BuildForumListing(Forum forum)
        {
            return new ForumListingModel
            {
                Id = forum.Id,
                Name = forum.Title,
                Description = forum.Description,
                ImageUrl = forum.ImageUrl
            };
        }
    }
}