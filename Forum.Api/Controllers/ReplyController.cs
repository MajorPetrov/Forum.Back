using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using ForumJV.Data.Models;
using ForumJV.Data.Services;
using ForumJV.Models.Reply;
using ForumJV.Extensions;

namespace ForumJV.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public class ReplyController : Controller
    {
        private readonly IPost _postService;
        private readonly IPostReply _replyService;
        private readonly INotification _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApplicationUser> _logger;

        public ReplyController(IPost postService, IPostReply replyService, INotification notificationService,
            UserManager<ApplicationUser> userManager, ILogger<ApplicationUser> logger)
        {
            _postService = postService;
            _replyService = replyService;
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Renvoie la vue d'un message en fonction de l'identifiant passé en paramètre.
        /// </summary>
        /// <param name="id">Un entier correspondant à l'identifiant du message</param>
        /// <returns>Une vue représentant le message</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Index(int id)
        {
            var reply = await _replyService.GetById(id);

            if (reply == null)
                return NotFound(new { error = $"La réponse d'identifiant : '{id}' n'existe pas." });

            var userRoles = await _userManager.GetRolesAsync(reply.User);
            var model = new PostReplyModel
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
            };

            return Json(model);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AddReply(NewReplyModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{_userManager.GetUserId(User)}'." });

            if (user.LockoutEnd != null)
                return Json(new { error = $"{user.UserName} est banni" });

            if (await _postService.IsLocked(model.PostId) && !User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
                return Forbid();
            // return Json(new { error = "Vous n'avez pas la permission pour cela" });

            var reply = await BuildReply(model, user);

            try
            {
                await _replyService.Create(reply);
                await _postService.UpdateLastReplyDate(reply.Post.Id);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de poster la réponse {reply.Id} : {exception.InnerException}" });
            }

            var mentions = await FindMentions(reply.Content);

            if (mentions.Any())
            {
                var notifications = BuildNotifications(user, mentions, reply);

                try
                {
                    await _notificationService.Create(notifications);
                }
                catch (Exception exception)
                {
                    return BadRequest(new { error = $"Impossible de notifier l'utilisateur {mentions} : {exception.InnerException}" });
                }
            }

            return Json(new { id = reply.Post.Id });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Modify(ModifyReplyModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            var user = await _userManager.GetUserAsync(User);
            var reply = await _replyService.GetById(model.Id);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{user.Id}'." });

            if (user.LockoutEnd != null)
                return Json(new { error = $"{user.UserName} est banni" });

            if (reply == null)
                return NotFound(new { error = $"La réponse d'identifiant : '{model.Id}' n'existe pas." });

            if (!user.Id.Equals(reply.UserId) && !User.IsInRole("Administrator"))
                return Forbid();
            // return Json(new { error = "Vous n'avez pas la permission pour cela" });

            try
            {
                await _replyService.Edit(model.Id, model.Content);
                await _postService.UpdateLastReplyDate(reply.PostId);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de modifier la réponse {reply.Id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a modifié la réponse {reply.Id}");

            return Json(new { id = model.Id });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Delete(int id)
        {
            var reply = await _replyService.GetById(id);

            if (reply == null)
                return NotFound(new { error = $"La réponse d'identifiant : '{id}' n'existe pas." });

            if (!User.Identity.Name.Equals(reply.User.UserName) && !User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
                return Forbid();
            // return Json(new { error = "Vous n'avez pas la permission pour cela" });

            try
            {
                var notifsToDelete = await _notificationService.GetByReplyId(id);

                if (notifsToDelete.Any())
                    await _notificationService.DeleteMany(notifsToDelete);

                await _replyService.Delete(id);
                await _postService.UpdateLastReplyDate(reply.PostId);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de supprimer la réponse {id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a supprimé la réponse {reply.Id}");

            return Json(new { id = reply.PostId });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Pin(int id)
        {
            var reply = await _replyService.GetById(id);

            if (reply == null)
                return NotFound(new { error = $"La réponse d'identifiant : '{id}' n'existe pas." });

            var userId = _userManager.GetUserId(User);

            if (!userId.Equals(reply.Post.UserId) && !User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
                return Forbid();
            // return Json(new { error = "Vous n'avez pas la permission pour cela" });

            try
            {
                await _replyService.Pin(id);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible d'épingler la réponse {id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a épinglé la réponse {reply.Id}");

            return Json(new { id = reply.PostId });
        }

        private async Task<PostReply> BuildReply(NewReplyModel reply, ApplicationUser user)
        {
            return new PostReply
            {
                Content = reply.Content,
                IpAddress = HttpContext.GetRemoteIPAddress().ToString(),
                Created = DateTime.UtcNow,
                User = user,
                Post = await _postService.GetById(reply.PostId)
            };
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

        private IEnumerable<Notification> BuildNotifications(ApplicationUser user, IEnumerable<ApplicationUser> mentionedUsers, PostReply reply)
        {
            var notifications = new List<Notification>();

            foreach (var mention in mentionedUsers)
            {
                notifications.Add(new Notification
                {
                    User = user,
                    Created = DateTime.UtcNow,
                    MentionedUserId = mention.Id,
                    ReplyId = reply.Id
                });
            }

            return notifications;
        }
    }
}