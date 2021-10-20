using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ForumJV.Data.Models;
using ForumJV.Data.Services;
using ForumJV.Models.ApplicationUser;
using ForumJV.Models.Badge;

namespace ForumJV.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationUser _userService;
        private readonly IBadge _badgeService;
        private readonly ILogger<ApplicationUser> _logger;

        public ProfileController(UserManager<ApplicationUser> userManager, IApplicationUser userService,
            IBadge badgeService, ILogger<ApplicationUser> logger)
        {
            _userManager = userManager;
            _userService = userService;
            _badgeService = badgeService;
            _logger = logger;
        }

        /// <summary>
        /// Invocable uniquement si l'utilisateur est authentifié.
        /// Retourne le profil public d'un utilisateur dont l'identifiant est passé en paramètre.
        /// </summary>
        /// <param name="userName">Identifiant de l'utilisateur</param>
        /// <returns>Une vue sur le profil public de l'utilisateur</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> Detail(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null)
                return NotFound(new { error = "Cet utilisateur n'existe pas" });

            var userRoles = await _userManager.GetRolesAsync(user);
            var userBadges = await _badgeService.GetUserBadges(user.Id);
            var badges = await BuildBadges(userBadges);
            var model = new ProfileModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Cancer = user.Cancer,
                ProfileImageUrl = user.ProfileImageUrl,
                Signature = user.Signature,
                MemberSince = user.MemberSince,
                IsBanned = await IsBanned(user),
                Role = userRoles.First(),
                Badges = badges
            };

            return Json(model);
        }

        /// <summary>
        /// Les fichiers mis en ligne en utilisant IFormFile sont mis en mémoire tampon ou sur le disque du serveur
        /// avant d'être traités. Dans la méthode, les contenus IFormFile sont accessibles sous forme de flux.
        /// Outre le système de fichier local, les fichiers peuvent être transférés vers le stockage Azure Blob ou Entity Framework.
        /// </summary>
        /// <param name="file">Fichier à mettre en ligne</param>
        /// <returns>Le résultat de la méthode Detail avec comme paramètre l'identifiant de l'utilisateur de l'avatar</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Aucun fichier sélectionné" });

            if (file.Length > 1000 * 1024)
                return BadRequest(new { error = "La taille maximale est de 1 Mo" });

            var extensions = new string[] { ".jpg", ".jpeg", ".png", ".gif" };

            if (!extensions.Contains(Path.GetExtension(file.FileName)))
                return BadRequest(new { error = $"Seuls les fichiers de format {String.Join(String.Empty, extensions)} sont acceptés" });

            // La partie supérieure est potentiellement à changer au profit d'une simple vérification de modèle (ProfileImageModel)

            var userId = _userManager.GetUserId(User);
            var pathToImages = $"/images/users/{userId}/";
            var applicationDirectory = Directory.GetCurrentDirectory() + "/wwwroot/";

            if (!Directory.Exists(pathToImages))
                Directory.CreateDirectory(applicationDirectory + pathToImages);

            if (Directory.EnumerateFiles(applicationDirectory + pathToImages).Any())
            {
                var directoryInfo = new DirectoryInfo(applicationDirectory + pathToImages);

                foreach (var image in directoryInfo.EnumerateFiles())
                {
                    image.Delete();
                }
            }

            using (var stream = new FileStream(applicationDirectory + pathToImages + file.FileName, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                await _userService.SetProfileImageAsync(userId, pathToImages + file.FileName);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de mettre à jour l'image de profil de l'utilisateur {userId} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a changé sa pdp");

            return Json(new { url = pathToImages + file.FileName });
        }

        /// <summary>
        /// Met à jour la signature de l'utilisateur en base de donnée et actualise la page.
        /// </summary>
        /// <param name="signature">La nouvelle signature du profil utilisateur</param>
        /// <returns>Le résultat de la méthode Detail avec comme paramètre l'identifiant de l'utilisateur</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateSignature(string signature)
        {
            const int maxLengthSignature = 100;

            if (signature.Length > maxLengthSignature)
                return BadRequest(new { error = $"La signature peut comporter au maximum {maxLengthSignature} caractères" });

            var userId = _userManager.GetUserId(User);

            try
            {
                await _userService.UpdateSignature(userId, signature);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de changer la signature de l'utilisateur {userId} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a changé sa signature");

            return Json(new { id = userId });
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator, Moderator")]
        public async Task<IActionResult> AddBadge(string userId, int badgeId)
        {
            var user = await _userService.GetById(userId);
            var badge = await _badgeService.GetById(badgeId);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{userId}'." });

            if (badge == null)
                return NotFound(new { error = $"Impossible de charger le badge d'identifiant : '{badgeId}'." });

            try
            {
                await _badgeService.AssignBadgeToUser(userId, badgeId);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible d'ajouter le badge {badgeId} à l'utilisateur {userId} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a ajouté le badge {badge.Name} à {user.UserName}");

            return Json(new { success = userId });
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator, Moderator")]
        public async Task<IActionResult> RemoveBadge(string userId, int badgeId)
        {
            var user = await _userService.GetById(userId);
            var badge = await _badgeService.GetById(badgeId);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{userId}'." });

            if (badge == null)
                return NotFound(new { error = $"Impossible de charger le badge d'identifiant : '{badgeId}'." });

            try
            {
                await _badgeService.RemoveBadgeFromUser(userId, badgeId);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de retirer le badge {badgeId} à l'utilisateur {userId} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a retiré le badge {badge.Name} à {user.UserName}");

            return Json(new { id = userId });
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator, Moderator")]
        public async Task<IActionResult> Ban(string id)
        {
            var user = await _userService.GetById(id);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{id}'." });

            if (user.LockoutEnd != null)
                return BadRequest(new { error = "Cet utilisateur est déjà banni" });

            try
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTime.MaxValue);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de débannir l'utilisateur '{id}' : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a banni {user.UserName}");

            return Json(new { success = "L'utilisateur est banni" });
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Unban(string id)
        {
            var user = await _userService.GetById(id);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur {id}" });

            try
            {
                await _userService.Unban(id);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de bannir l'utilisateur '{id}' : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a réhabilité {user.UserName}");

            return Json(new { success = "L'utilisateur est débanni" });
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userService.GetById(id);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{id}'." });

            try
            {
                await _userManager.DeleteAsync(user);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de supprimer l'utilisateur {id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a détruit {user.UserName}");

            return Json(new { success = "L'utilisateur est supprimé" });
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ChangeRole(string id, string role)
        {
            if (role.ToLower().Equals("administrator"))
                return Forbid();
            // return BadRequest(new { error = "Vous ne pouvez pas nommer un administrateur" });

            var user = await _userService.GetById(id);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{id}'." });

            if (await _userManager.IsInRoleAsync(user, "Administrator"))
                return Forbid();
            // return Json(new { error = $"Impossible de rétrograder un administrateur" });

            try
            {
                await _userService.ChangeRole(id, role);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Impossible de changer le rôle de l'utilisateur {id} : {exception.InnerException}" });
            }

            _logger.LogInformation($"{User.Identity.Name} a nommé {user.UserName} {role}");

            return Json(new { id = id });
        }

        private async Task<IEnumerable<BadgeModel>> BuildBadges(IEnumerable<UserBadge> userBadges)
        {
            var badges = new List<BadgeModel>();

            foreach (var badge in userBadges)
                badges.Add(await BuildBadge(badge));

            return badges;
        }

        private async Task<BadgeModel> BuildBadge(UserBadge userBadge)
        {
            var badge = await _badgeService.GetById(userBadge.BadgeId);

            return new BadgeModel
            {
                Id = badge.Id,
                Name = badge.Name,
                Description = badge.Description,
                ImageUrl = badge.ImageUrl,
                ObtainingDate = userBadge.ObtainingDate
            };
        }

        private async Task<bool> IsBanned(ApplicationUser user)
        {
            var lockout = await _userManager.GetLockoutEndDateAsync(user);

            return lockout != null;
        }
    }
}