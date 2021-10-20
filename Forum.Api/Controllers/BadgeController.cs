using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using ForumJV.Data.Models;
using ForumJV.Data.Services;
using ForumJV.Models.Badge;

namespace ForumJV.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public class BadgeController : Controller
    {
        private readonly IBadge _badgeService;
        private readonly ILogger<ApplicationUser> _logger;

        public BadgeController(IBadge badgeService, ILogger<ApplicationUser> logger)
        {
            _badgeService = badgeService;
            _logger = logger;
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator, Moderator")]
        public async Task<IActionResult> Index()
        {
            var badges = await _badgeService.GetAll();
            var sortedBadges = badges.OrderBy(badge => badge.Id)
                .Select(b => new BadgeModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    ImageUrl = b.ImageUrl
                });

            var model = new BadgeListingModel { Badges = sortedBadges };

            return Json(model);
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(BadgeModel model, IFormFile file)
        {
            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            var badge = BuildBadge(model);
            var pathToImages = "/images/badges/" + file.FileName;

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Aucun fichier sélectionné" });

            using (var stream = new FileStream(Directory.GetCurrentDirectory() + "/wwwroot/" + pathToImages, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            badge.ImageUrl = pathToImages;

            try
            {
                await _badgeService.Create(badge);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de créer le badge {badge.Id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a créé le badge {badge.Name}");

            return Json(new { success = "Badge créé" });
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _badgeService.Delete(id);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de supprimer le badge {id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a supprimé le badge {id}");

            return Json(new { success = "Badge supprimé" });
        }

        private Badge BuildBadge(BadgeModel model)
        {
            return new Badge
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                ImageUrl = model.ImageUrl
            };
        }
    }
}