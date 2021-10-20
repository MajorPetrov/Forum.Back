using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ForumJV.Data.Models;
using ForumJV.Data.Services;
using ForumJV.Models.Notification;
// using ForumJV.Application.Core.Domain.Services;

namespace ForumJV.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public class NotificationController : Controller
    {
        private readonly INotification _notificationService;
        private readonly IPost _postService;
        private readonly IPostReply _replyService;
        private readonly IApplicationUser _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApplicationUser> _logger;
        // private readonly ICrudService<Forum.Application.Domain.Models.Notification> _crud;

        public NotificationController(INotification notificationService, IPost postService, IPostReply replyService,
            IApplicationUser userService, UserManager<ApplicationUser> userManager, ILogger<ApplicationUser> logger)
        {
            _notificationService = notificationService;
            _postService = postService;
            _replyService = replyService;
            _userService = userService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Renvoie les notifications au format JSON de l'utilisateur dont l'identifiant est passé en paramètre
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <returns>Un objet JSON contenant les notifications</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Index(string userId)
        {
            var notifications = await _notificationService.GetUserNotifications(userId);

            if (notifications == null)
                return Json(new { error = $"L'utilisateur {userId} n'a pas de notification" });

            notifications.OrderBy(notif => notif.Created);

            var notificationListing = new List<NotificationModel>();

            foreach (var notif in notifications)
                notificationListing.Add(await BuildNotificationListing(notif));

            var model = new NotificationListingModel { Notifications = notificationListing };

            return Json(model);
        }

        /// <summary>
        /// Supprime la notification dont l'identifiant est passé en paramètre
        /// </summary>
        /// <param name="id">Identifiant de la notification</param>
        /// <returns>Code de retour de l'opération</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _notificationService.GetById(id);

            if (notification == null)
                return Json(new { error = $"La notification d'identifiant : '{id}' n'existe pas." });

            try
            {
                await _notificationService.Delete(id);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de supprimer la notification {id} : {exception.Message}" });
            }

            _logger.LogInformation($"La notification {id} a été supprimée");

            return Json(new { success = "Notification supprimée" });
        }

        /// <summary>
        /// Supprime toutes les notifications l'utilisateur actuellement connecté
        /// </summary>
        /// <returns>Code de retour de l'opération</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteAll()
        {
            var userId = _userManager.GetUserId(User);

            try
            {
                await _notificationService.DeleteAll(userId);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de supprimer les notifications de l'utilisateur {userId} : {exception.Message}" });
            }

            _logger.LogInformation($"Les notifications de l'utilisateur {userId} ont étés supprimées");

            return Json(new { success = "Notifications supprimées" });
        }

        /// <summary>
        /// Ajoute un sujet dans la liste des favoris d'un utilisateur
        /// </summary>
        /// <param name="postId">Identifiant du sujet à ajouter en favori</param>
        /// <param name="userId">Identifiant de l'utilisateur auquel ajouter le favori</param>
        /// <returns>Code de retour de l'opération</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Favorite(int postId, string userId)
        {
            var post = await _postService.GetById(postId);
            var user = await _userManager.GetUserAsync(User);

            if (post == null)
                return NotFound(new { error = $"Le post d'identifiant : '{postId}' n'existe pas." });

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{user.Id}'." });

            if (!await _postService.IsFavorite(postId, userId))
            {
                try
                {
                    await _postService.Favorite(postId, userId);
                }
                catch (Exception exception)
                {
                    return BadRequest(new { error = $"Impossible de mettre le sujet {postId} en favori : {exception.Message}" });
                }

                _logger.LogInformation($"{User.Identity.Name} a mis le sujet {postId} en favori");
            }

            return Json(new { id = postId });
        }

        /// <summary>
        /// Supprime un sujet de la liste des favoris d'un utilisateur
        /// </summary>
        /// <param name="postId">Identifiant du sujet à supprimer des favoris</param>
        /// <param name="userId">Identifiant de l'utilisateur duquel supprimer le favori</param>
        /// <returns>Code de retour de l'opération</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Unfavorite(int postId, string userId)
        {
            var post = await _postService.GetById(postId);
            var user = await _userService.GetById(userId);

            if (post == null)
                return NotFound(new { error = $"Le post d'identifiant : '{postId}' n'existe pas." });

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{userId}'." });

            try
            {
                await _postService.Unfavorite(postId, userId);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de retirer le sujet {postId} des favoris : {exception.Message}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a retiré le sujet {postId} de ses favoris");

            return Json(new { id = postId });
        }

        /// <summary>
        /// Ajoute un sujet dans la liste des suivis d'un utilisateur
        /// </summary>
        /// <param name="postId">Identifiant du sujet à ajouter en suivi</param>
        /// <param name="userId">Identifiant de l'utilisateur auquel ajouter le suivi</param>
        /// <returns>Code de retour de l'opération</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Follow(int postId, string userId)
        {
            var post = await _postService.GetById(postId);
            var user = await _userService.GetById(userId);

            if (post == null)
                return NotFound(new { error = $"Le post d'identifiant : '{postId}' n'existe pas." });

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{userId}'." });

            if (!await _postService.IsFollowed(postId, userId))
            {
                try
                {
                    await _postService.Follow(postId, userId);
                }
                catch (Exception exception)
                {
                    return BadRequest(new { error = $"Impossible de mettre le sujet {postId} en abonnement : {exception.Message}" });
                }

                _logger.LogInformation($"{User.Identity.Name} a mis le sujet {postId} en abonnement");
            }

            return Json(new { id = postId });
        }

        /// <summary>
        /// Supprime un sujet de la liste des suivis d'un utilisateur
        /// </summary>
        /// <param name="postId">Identifiant du sujet à supprimer des suivis</param>
        /// <param name="userId">Identifiant de l'utilisateur duquel supprimer le suivi</param>
        /// <returns>Code de retour de l'opération</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Unfollow(int postId, string userId)
        {
            var post = await _postService.GetById(postId);
            var user = await _userService.GetById(userId);

            if (post == null)
                return NotFound(new { error = $"Le post d'identifiant : '{postId}' n'existe pas." });

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{userId}'." });

            try
            {
                await _postService.Unfollow(postId, userId);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de retirer le sujet {postId} des abonnements : {exception.Message}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a retiré le sujet {postId} de ses abonnements");

            return Json(new { id = postId });
        }

        private async Task<NotificationModel> BuildNotificationListing(Notification notification)
        {
            var model = new NotificationModel
            {
                Id = notification.Id,
                AuthorId = notification.User.Id,
                AuthorName = notification.User.UserName,
                MentionedUserId = notification.MentionedUserId,
                MentionedUserName = notification.MentionedUser.UserName,
                Created = notification.Created
            };

            // Peut-être à refactoriser...
            if (notification.PostId != null)
            {
                var post = await _postService.GetById(notification.PostId.Value);
                model.TitleContent = post.Title;
                model.ContentId = post.Id;
                model.PageNumber = 1;
            }
            else
            {
                var reply = await _replyService.GetById(notification.ReplyId.Value);
                model.TitleContent = reply.Post.Title;
                model.ContentId = reply.Id;
                model.PageNumber = await _replyService.GetReplyPage(reply);
            }

            return model;
        }
    }
}