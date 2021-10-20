using System;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ForumJV.Data.Models;
using ForumJV.Data.Services;
using ForumJV.Models.Manage;

namespace ForumJV.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAccount _accountService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ApplicationUser> _logger;
        private readonly UrlEncoder _urlEncoder;
        private const string _authenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        private const string _recoveryCodesKey = nameof(_recoveryCodesKey);

        public ManageController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IAccount accountService,
            IEmailSender emailSender, ILogger<ApplicationUser> logger, UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _accountService = accountService;
            _emailSender = emailSender;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Index(IndexModel model)
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

            if (model.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);

                // vérifier que l'utilisateur a son email non confirmé après le changement
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var codeConverted = Uri.EscapeDataString(code);
                var callbackUrl = "https://" + HttpContext.Request.Host + "/valider-mon-email/" + user.Id + "/" + codeConverted;

                try
                {
                    await _emailSender.SendEmailAsync(model.Email, "Email de confirmation Forum",
                        $"Veuillez confirmer votre compte en cliquant ici : {callbackUrl}");
                }
                catch (Exception exception)
                {
                    return BadRequest(new { error = exception.InnerException });
                }

                if (!setEmailResult.Succeeded)
                    return BadRequest(new { error = $"Une erreur est survenue durant le changement d'email pour l'utilisateur d'identifiant '{user.Id}'." });
            }

            return Json("Un e-mail de confirmation vous a été envoyé");
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
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

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                var legacyPasswordValid = _accountService.VerifyLegacyPassword(model.OldPassword, user.PasswordHash);

                if (!legacyPasswordValid)
                    return BadRequest(new { error = "Mot de passe actuel incorrect" });

                var result = await _userManager.RemovePasswordAsync(user);

                if (!result.Succeeded)
                    return BadRequest(new { error = $"Impossible de supprimer le mot de passe legacy de {user.UserName}" });

                result = await _userManager.AddPasswordAsync(user, model.NewPassword);

                if (!result.Succeeded)
                    return BadRequest(new { error = $"Impossible d'ajouter un mot de passe V2 à {user.UserName}" });
            }

            try
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = exception.InnerException });
            }

            _logger.LogInformation($"{user.UserName} a modifié son mot de passe.");

            return Json(new { success = "Votre mot de passe a bien été modifié." });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> TwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{_userManager.GetUserId(User)}'." });

            var model = new TwoFactorAuthenticationModel
            {
                HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null,
                Is2FAEnabled = user.TwoFactorEnabled,
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user),
            };

            return Json(model);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Disable2fa()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{_userManager.GetUserId(User)}'." });

            if (!user.TwoFactorEnabled)
                return BadRequest(new { error = "Vous n'avez pas activé la double authentification" });

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);

            if (!disable2faResult.Succeeded)
                return BadRequest(new { error = "Une erreur est survenue lors de la désactivation de la 2FA" });

            _logger.LogInformation($"{user.UserName} a désactivé la 2FA");

            return Json(new { success = "Vous avez desactivé la double authentification" });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> GetEnableAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{_userManager.GetUserId(User)}'." });

            var model = new EnableAuthenticatorModel();

            try
            {
                await LoadSharedKeyAndQrCodeUriAsync(user, model);
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = exception.InnerException });
            }

            return Json(model);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorModel model)
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

            // remplacement des espaces par des tirets
            var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                try
                {
                    await LoadSharedKeyAndQrCodeUriAsync(user, model);
                }
                catch (Exception exception)
                {
                    return BadRequest(new { error = exception.InnerException });
                }

                return BadRequest(new { error = "Le code de verification est invalide." });
            }

            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);

            if (!result.Succeeded)
                return BadRequest(new { error = $"Impossible d'activer la 2FA" });

            _logger.LogInformation($"{user.UserName} a activé la 2FA");

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            TempData[_recoveryCodesKey] = recoveryCodes.ToArray();

            return await ShowRecoveryCodes();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ShowRecoveryCodes()
        {
            var recoveryCodes = (string[])TempData[_recoveryCodesKey];

            if (recoveryCodes == null)
                return await TwoFactorAuthentication();

            var model = new GenerateRecoveryCodesModel { RecoveryCodes = recoveryCodes };

            return Json(model);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ResetAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return BadRequest(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{_userManager.GetUserId(User)}'." });

            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);

            if (!result.Succeeded)
                return BadRequest(new { error = "Impossible d'activer la 2FA" });

            result = await _userManager.ResetAuthenticatorKeyAsync(user);

            if (!result.Succeeded)
                return BadRequest(new { error = "Impossible de réinitialiser la 2FA" });

            _logger.LogInformation($"{user.UserName} a réinitialisé la 2FA");

            return Json(new { success = "Votre clef d'authentificateur à bien été réinitialisé, vous pouvez suppprimer votre configuration dans votre application." });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{_userManager.GetUserId(User)}'." });

            if (!user.TwoFactorEnabled)
                return BadRequest(new { error = "Vous n'avez pas activé la double authentification" });

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            var model = new GenerateRecoveryCodesModel { RecoveryCodes = recoveryCodes.ToArray() };

            _logger.LogInformation($"{user.UserName} a généré des codes de récupération pour la 2FA");

            return Json(model);
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;

            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }

            if (currentPosition < unformattedKey.Length)
                result.Append(unformattedKey.Substring(currentPosition));

            return result.ToString().ToLowerInvariant();
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user, EnableAuthenticatorModel model)
        {
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);

                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            model.SharedKey = FormatKey(unformattedKey);
            model.AuthenticatorUri = GenerateQrCodeUri(user.UserName, unformattedKey);
        }

        private string GenerateQrCodeUri(string userName, string unformattedKey)
        {
            return string.Format(_authenticatorUriFormat, _urlEncoder.Encode("Forum"),
                _urlEncoder.Encode(userName), unformattedKey);
        }
    }
}
