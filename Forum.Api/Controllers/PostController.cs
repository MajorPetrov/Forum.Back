using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using ForumJV.Data;
using ForumJV.Data.Models;
using ForumJV.Data.Services;
using ForumJV.Models.Post;
using ForumJV.Models.Reply;
using ForumJV.Models.Poll;
using ForumJV.Extensions;
// using Forum.Application.Core.UnitOfWork;
// using Forum.Application.Core.Domain.Services;

namespace ForumJV.Controllers
{
    [Route("api/[controller]")]
    public class PostController : Controller
    {
        private readonly IPost _postService;
        private readonly IPostReply _replyService;
        private readonly IForum _forumService;
        private readonly IPoll _pollService;
        private readonly INotification _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApplicationUser> _logger;
        // private readonly ICrudService<Forum.Application.Domain.Models.Post> _postServiceCrud;
        // private readonly ICrudService<Forum.Application.Domain.Models.Poll> _pollServiceCrud;
        // private readonly IUnitOfWork _unitOfWork;
        private IMemoryCache _cache;

        public PostController(IPost postService, IPostReply replyService, IForum forumService, IPoll pollService,
            INotification notificationService, UserManager<ApplicationUser> userManager, ILogger<ApplicationUser> logger, IMemoryCache cache)
        {
            _postService = postService;
            _replyService = replyService;
            _forumService = forumService;
            _pollService = pollService;
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Renvoie la vue d'un sujet en fonction de l'identifiant passé en paramètre.
        /// </summary>
        /// <param name="id">Un entier correspondant à l'identifiant du sujet</param>
        /// <param name="pageNumber">Numéro de la page à afficher</param>
        /// <returns>Une vue représentant un sujet</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Index(int id, int pageNumber = 1)
        {
            Post post;
            IEnumerable<PostReply> replies;
            IEnumerable<PostReply> pinnedReplies = new List<PostReply>();

            if (!User.Identity.IsAuthenticated)
            {
                if (!_cache.TryGetValue(CacheKeys.Post + id + pageNumber, out post))
                {
                    post = await _postService.GetById(id);

                    _cache.Set(CacheKeys.Post + id + pageNumber, post, CacheKeys.Expiration);
                }

                if (!_cache.TryGetValue(CacheKeys.RepliesByPage + id + pageNumber, out replies))
                {
                    replies = await _replyService.GetRepliesByPage(post.Id, pageNumber);
                    pinnedReplies = await _replyService.GetPinnedRepliesByPost(post.Id);

                    _cache.Set(CacheKeys.RepliesByPage + id + pageNumber, replies, CacheKeys.Expiration);
                }
            }
            else
            {
                post = await _postService.GetById(id);
                replies = await _replyService.GetRepliesByPage(post.Id, pageNumber);
                pinnedReplies = await _replyService.GetPinnedRepliesByPost(post.Id);
            }

            if (post == null)
                return NotFound(new { error = $"Le sujet d'identifiant : '{id}' n'existe pas." });

            var userRoles = await _userManager.GetRolesAsync(post.User);

            if (pageNumber == 1)
                replies = pinnedReplies.Concat(replies);

            var model = new PostIndexModel
            {
                Id = post.Id,
                Title = post.Title,
                AuthorId = post.UserId,
                AuthorName = post.User.UserName,
                AuthorImageUrl = post.User.ProfileImageUrl,
                AuthorCancer = post.User.Cancer,
                AuthorSignature = post.User.Signature,
                Created = post.Created,
                Content = post.Content,
                Type = post.Type,
                AuthorRole = userRoles.FirstOrDefault(),
                Replies = await BuildPostReplies(replies),
                RepliesCount = await _replyService.GetRepliesCountByPost(post.Id),
                IsPinned = post.IsPinned,
                IsLocked = await _postService.IsLocked(post.Id),
                ForumId = post.ForumId,
                ForumName = post.Forum.Title
            };

            if (post.Poll != null)
                model.Poll = await BuildPollModel(post.Poll);

            if (await _postService.IsLocked(post.Id))
            {
                var archivedPost = await _postService.GetArchivedPostById(id);
                model.LockReason = archivedPost.Reason;
            }

            return Json(model);
        }

        /// <summary>
        /// Crée un nouveau sujet à partir du modèle passé en paramètre.
        /// </summary>
        /// <param name="postModel">Objet NewPostModel représentant un nouveau sujet.</param>
        /// <param name="pollModel">Objet NewPollModel représentant un nouveau sondage. Ce paramètre est facultatif.</param>
        /// <param name="hasPoll">booléen indiquant si le sujet est censé inclure un sondage ou non.</param>
        /// <returns>Un objet JSON avec les attributs définis dans le modèle.</returns>
        [HttpPost("[action]")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPost(NewPostModel postModel, NewPollModel pollModel = null, bool hasPoll = false)
        {
            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{userId}'." });

            if (user.LockoutEnd != null)
                return Json(new { error = $"{user.UserName} est banni" });

            var post = await BuildPost(postModel, user);

            if (hasPoll)
            {
                if (pollModel.Question == null)
                    return Json(new { error = "Le sondage doit avoir une question" });

                if (pollModel.Options.Count <= 1)
                    return Json(new { error = "Le sondage doit avoir au moins 2 réponses" });

                if (pollModel.Options.Count > 20)
                    return Json(new { error = "Le sondage doit avoir au plus 20 réponses" });

                if (pollModel.Options.Contains(null))
                    return Json(new { error = "Les réponses ne peuvent pas être vides" });

                post.Poll = BuildPoll(pollModel);
            }

            try
            {
                await _postService.Create(post);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de poster le sujet {post.Id} : {exception.InnerException}" });
            }

            var mentions = await FindMentions(post.Content);

            if (mentions.Any())
            {
                var notifications = BuildNotifications(user, mentions, post);

                try
                {
                    await _notificationService.Create(notifications);
                }
                catch (Exception exception)
                {
                    return BadRequest(new { error = $"Impossible de notifier l'utilisateur {mentions} : {exception.InnerException}" });
                }
            }

            return Json(new { id = post.Id });
        }

        /// <summary>
        /// Modifie le titre d'un sujet existant à partir du modèle passé en paramètre.
        /// </summary>
        /// <param name="model">Objet NewPostModel représentant un le sujet à modifier.</param>
        /// <returns>Un objet JSON avec les attributs définis dans le modèle.</returns>
        [HttpPost("[action]")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModifyTitle(NewPostModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            var post = await _postService.GetById(model.Id);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{userId}'." });

            if (user.LockoutEnd != null)
                return Json(new { error = $"{user.UserName} est banni" });

            if (post == null)
                return NotFound(new { error = $"Le sujet d'identifiant : '{model.Id}' n'existe pas." });

            if (!userId.Equals(post.User.Id) && !User.IsInRole("Administrator") && !User.IsInRole("Moderator"))
                return Forbid();
            // return Forbid(new { error = "Vous n'avez pas la permission pour cela" });

            try
            {
                await _postService.EditPostTitle(model.Id, model.Title);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de modifier le titre du sujet {post.Id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{user.UserName} a modifié le titre du sujet {post.Id}");

            return Json(new { success = "Le titre est bien modifié" });
        }

        /// <summary>
        /// Modifie le contenu d'un sujet existant à partir du modèle passé en paramètre.
        /// </summary>
        /// <param name="model">Objet NewPostModel représentant un le sujet à modifier.</param>
        /// <returns>Un objet JSON avec les attributs définis dans le modèle.</returns>
        [HttpPost("[action]")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModifyContent(NewPostModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            var user = await _userManager.GetUserAsync(User);
            var post = await _postService.GetById(model.Id);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{user.Id}'." });

            if (user.LockoutEnd != null)
                return Json(new { error = $"{user.UserName} est banni" });

            if (post == null)
                return NotFound(new { error = $"Le sujet d'identifiant : '{model.Id}' n'existe pas." });

            if (!user.Id.Equals(post.User.Id) && !User.IsInRole("Administrator"))
                return Forbid();
            // return Json(new { error = "Vous n'avez pas la permission pour cela" });

            try
            {
                await _postService.EditPostContent(model.Id, model.Content);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de modifier le sujet {post.Id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{user.UserName} a modifié le contenu du sujet {post.Id}");

            return Json(new { success = "Le sujet est modifié" });
        }

        [HttpPost("[action]")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _postService.GetById(id);

            if (post == null)
                return NotFound(new { error = $"Le sujet d'identifiant : '{id}' n'existe pas." });

            if (!User.Identity.Name.Equals(post.User.UserName) && !User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
                return Forbid();
            // return Json(new { error = "Vous n'avez pas la permission pour cela" });

            try
            {
                var notifsToDelete = await _notificationService.GetByPostId(id);

                if (notifsToDelete.Any())
                    await _notificationService.DeleteMany(notifsToDelete);

                if (post.Poll != null)
                    await _pollService.Delete(post.Poll.Id);

                await _postService.Delete(id);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de supprimer le sujet {id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a supprimé le sujet {post.Id}");

            return Json(new { success = "Le sujet est bien supprimé", id = post.ForumId });
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Moderator, Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pin(int id)
        {
            var post = await _postService.GetById(id);

            if (post == null)
                return NotFound(new { error = $"Le sujet d'identifiant : '{id}' n'existe pas." });

            try
            {
                await _postService.Pin(id);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de modifier l'épingle du sujet {id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a épinglé le sujet {id}");

            return Json(new { id = id });
        }

        [HttpPost("[action]")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(int id, string message)
        {
            var post = await _postService.GetById(id);

            if (post == null)
                return NotFound(new { error = $"Le sujet d'identifiant : '{id}' n'existe pas." });

            var user = await _userManager.GetUserAsync(User);

            if (!user.Id.Equals(post.User.Id) && !User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
                return Forbid();
            // return Json(new { error = "Vous n'avez pas la permission pour cela" });

            if (!await _postService.IsLocked(id))
            {
                var archivedPost = new ArchivedPost
                {
                    PostId = id,
                    Reason = message,
                    UserId = user.Id
                };

                try
                {
                    await _postService.Lock(archivedPost);
                }
                catch (Exception exception)
                {
                    return BadRequest(new { error = $"Impossible de verrouiller le sujet {id} : {exception.InnerException}" });
                }

                _logger.LogInformation($"{User.Identity.Name} a verrouillé le sujet {post.Id}");
            }

            return Json(new { id = id });
        }

        [HttpPost("[action]")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(int id)
        {
            var post = await _postService.GetById(id);

            if (post == null)
                return NotFound(new { error = $"Le sujet d'identifiant : '{id}' n'existe pas." });

            var archivedPost = await _postService.GetArchivedPostById(id);

            if (archivedPost == null)
                return Json(new { error = $"Le post d'identifiant : '{id}' n\'est pas verrouillé" });

            var userId = _userManager.GetUserId(User); // l'utilisateur connecté
            var user = await _userManager.FindByIdAsync(archivedPost.UserId); // l'utilisateur qui a verrouillé le sujet

            if (user == null)
                return Json(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{user.Id}'." });

            var userRole = await _userManager.GetRolesAsync(user);

            if (!User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
            {
                if (!userId.Equals(post.User.Id))
                {
                    return Forbid();
                    // return Json(new { error = "Vous n'êtes pas l'auteur de ce sujet" });
                }
                else if (userRole.First().Equals("Moderator") || userRole.First().Equals("Administrator"))
                {
                    return Forbid();
                    // return Json(new { error = $"Ce sujet a été verrouillé par un {userRole.First()}" });
                }
            }

            try
            {
                await _postService.Unlock(id);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de déverrouiller le sujet {id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a déverrouillé le sujet {post.Id}");

            return Json(new { id = post.Id });
        }

        [HttpPost("[action]")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vote(int optionId)
        {
            var userId = _userManager.GetUserId(User);
            var option = await _pollService.GetOptionById(optionId);
            var poll = await _pollService.GetById(option.PollId);

            if (option == null)
                return NotFound(new { error = $"L'option de vote {optionId} n'existe pas." });

            // potentiellement à optimiser
            foreach (var opt in poll.Options)
            {
                if (await _pollService.HasUserVoted(opt.Id, userId))
                    return Json(new { error = $"Vous avez déjà voté sur le sondage {option.PollId}" });
            }

            try
            {
                await _pollService.Vote(optionId, userId);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de voter pour l'option {optionId} : {exception.InnerException}" });
            }

            var model = await BuildPollModel(option.Poll);

            return Json(model);
        }

        private async Task<IEnumerable<PostReplyModel>> BuildPostReplies(IEnumerable<PostReply> replies)
        {
            var repliesModel = new List<PostReplyModel>();

            foreach (var reply in replies)
            {
                var userRoles = await _userManager.GetRolesAsync(reply.User);

                repliesModel.Add(new PostReplyModel
                {
                    Id = reply.Id,
                    AuthorId = reply.UserId,
                    AuthorName = reply.User.UserName,
                    AuthorImageUrl = reply.User.ProfileImageUrl,
                    AuthorCancer = reply.User.Cancer,
                    AuthorSignature = reply.User.Signature,
                    AuthorRole = userRoles.FirstOrDefault(),
                    Content = reply.Content,
                    IsPinned = reply.IsPinned,
                    PostId = reply.PostId,
                    PostType = reply.Post.Type,
                    Created = reply.Created
                });
            }

            return repliesModel;
        }

        private async Task<Post> BuildPost(NewPostModel post, ApplicationUser user)
        {
            return new Post
            {
                Title = post.Title,
                Content = post.Content,
                IpAddress = HttpContext.GetRemoteIPAddress().ToString(),
                Type = post.Type,
                Created = DateTime.UtcNow,
                LastReplyDate = DateTime.UtcNow,
                User = user,
                Forum = await _forumService.GetById(post.ForumId)
            };
        }

        private async Task<PollModel> BuildPollModel(Poll poll)
        {
            int votesCount = 0;

            foreach (var option in poll.Options)
                votesCount += await _pollService.GetVotesCountByOption(option.Id);

            return new PollModel
            {
                Question = poll.Question,
                Options = await BuildPollOptions(poll.Options),
                VotesCount = votesCount
            };
        }

        private async Task<IEnumerable<PollOptionModel>> BuildPollOptions(IEnumerable<PollOption> options)
        {
            var optionsModel = new List<PollOptionModel>();
            var userId = _userManager.GetUserId(User);

            foreach (var option in options)
            {
                optionsModel.Add(new PollOptionModel
                {
                    Id = option.Id,
                    Answer = option.Answer,
                    VotesCount = await _pollService.GetVotesCountByOption(option.Id),
                    Selected = await _pollService.HasUserVoted(option.Id, userId)
                });
            }

            return optionsModel;
        }

        private Poll BuildPoll(NewPollModel model)
        {
            var poll = new Poll { Question = model.Question };

            var options = model.Options.Select(option => new PollOption
            {
                Answer = option,
                PollId = poll.Id
            }).ToList();

            poll.Options = options;

            return poll;
        }

        private async Task<IEnumerable<ApplicationUser>> FindMentions(string content)
        {
            var users = new List<ApplicationUser>();
            var regex = new Regex("@(?<name>[^\\s]+)");

            var usersToNotify = regex.Matches(content)
                .Cast<Match>()
                .Select(m => m.Groups["name"].Value)
                .ToArray();

            foreach (var userName in usersToNotify)
                users.Add(await _userManager.FindByNameAsync(userName));

            return users;
        }

        private IEnumerable<Notification> BuildNotifications(ApplicationUser user, IEnumerable<ApplicationUser> mentionedUsers, Post post)
        {
            var notifications = new List<Notification>();

            foreach (var mention in mentionedUsers)
            {
                notifications.Add(new Notification
                {
                    User = user,
                    Created = DateTime.UtcNow,
                    MentionedUserId = mention.Id,
                    PostId = post.Id
                });
            }

            return notifications;
        }
    }
}