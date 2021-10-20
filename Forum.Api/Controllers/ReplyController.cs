using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Forum.Data.Models;
using Forum.Data.Services;
using Forum.Models.Reply;
using Forum.Extensions;

namespace Forum.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public class ReplyController : Controller
    {
        private readonly IPost _postService;
        private readonly IPostReply _replyService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApplicationUser> _logger;

        public ReplyController(IPost postService, IPostReply replyService,
            UserManager<ApplicationUser> userManager, ILogger<ApplicationUser> logger)
        {
            _postService = postService;
            _replyService = replyService;
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
                AuthorId = reply.User.Id,
                AuthorName = reply.User.UserName,
                AuthorImageUrl = reply.User.ProfileImageUrl,
                AuthorCancer = reply.User.Cancer,
                AuthorSignature = reply.User.Signature,
                AuthorRole = userRoles.FirstOrDefault(),
                Content = reply.Content,
                IsPinned = reply.IsPinned,
                PostId = reply.Post.Id,
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

            if (await _postService.IsLock(model.PostId) && !User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
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

            if (!user.Id.Equals(reply.User.Id) && !User.IsInRole("Administrator"))
                return Forbid();
            // return Json(new { error = "Vous n'avez pas la permission pour cela" });

            try
            {
                await _replyService.Edit(model.Id, model.Content);
                await _postService.UpdateLastReplyDate(reply.Post.Id);
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
                await _replyService.Delete(id);
                await _postService.UpdateLastReplyDate(reply.Post.Id);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de supprimer la réponse {id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a supprimé la réponse {reply.Id}");

            return Json(new { id = reply.Post.Id });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Pin(int id)
        {
            var reply = await _replyService.GetById(id);

            if (reply == null)
                return NotFound(new { error = $"La réponse d'identifiant : '{id}' n'existe pas." });

            var userId = _userManager.GetUserId(User);

            if (!userId.Equals(reply.Post.User.Id) && !User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
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

            return Json(new { id = reply.Post.Id });
        }

        private async Task<PostReply> BuildReply(NewReplyModel reply, ApplicationUser user)
        {
            var post = await _postService.GetById(reply.PostId);

            return new PostReply
            {
                Content = reply.Content,
                IpAddress = HttpContext.GetRemoteIPAddress().ToString(),
                Created = DateTime.Now,
                User = user,
                Post = post
            };
        }
    }
}