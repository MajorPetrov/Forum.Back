using System;
using System.Web;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using ForumJV.Data.Models;
using ForumJV.Data.Services;
using ForumJV.Data.Options;
using ForumJV.Models.Account;
using ForumJV.Extensions;

namespace ForumJV.Controllers
{
    [Route("api/[controller]")]
    [ValidateAntiForgeryToken]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAntiforgery _antiforgery;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ApplicationUser> _logger;
        private readonly IApplicationUser _userService;
        private readonly IAccount _accountService;
        private readonly IPost _postService;
        private readonly IPostReply _replyService;
        private readonly CaptchaKeys _options;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IAntiforgery antiforgery, IEmailSender emailSender,
            ILogger<ApplicationUser> logger, IApplicationUser userService, IAccount accountService, IPost postService, IPostReply replyService, IOptions<CaptchaKeys> optionsAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _antiforgery = antiforgery;
            _emailSender = emailSender;
            _logger = logger;
            _userService = userService;
            _accountService = accountService;
            _postService = postService;
            _replyService = replyService;
            _options = optionsAccessor.Value;
        }

        /// <summary>
        /// M??thode de connexion utilisant le nouveau chiffrement de mot de passe
        /// Prend en param??tre le mod??le de connexion.
        /// Selon le r??sultat d??livr?? par la requ??te de connexion, la m??thode renverra un r??sultat diff??rent.
        /// Si les identifiants sont corrects, elle renverra un JSON Web Token.
        /// Si le compte utilisateur est param??tr?? pour la Double authentification,
        /// la m??thode utilisant ce syst??me sera renvoy??e.
        /// Si l'utilisateur est banni de connexion, il sera renvoy?? ?? la m??thode associ??e.
        /// Dans tous les autres cas, la m??thode renverra une erreur au format JSON.
        /// </summary>
        /// <param name="model">Mod??le de connexion o?? les identifiants sont rentr??s</param>
        /// <returns>Sch??ma JSON</returns>
        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginModel model)
        {
            // Suppression du cookie existant pour une connexion optimale
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            var user = await _userManager.FindByNameAsync(model.UserName);
            var ipAddress = HttpContext.GetRemoteIPAddress().ToString();
            var errors = await CheckUserCredentials(user, model.Password, ipAddress);

            if (!string.IsNullOrEmpty(errors))
                return NotFound(new { error = errors });

            if (user.TwoFactorEnabled)
                return Json(new { success = "La double authentification est activ?? sur ce compte.", TwoFactorEnabled = true });

            // D??but de la co
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
                IssuedUtc = DateTime.UtcNow,
            };

            try
            {
                await _signInManager.SignInAsync(user, authProperties);
            }
            catch (Exception exception)
            {
                return BadRequest(new { errors = exception.InnerException });
            }
            // Fin de la co

            RefreshXsrfToken();
            _logger.LogInformation($"{user.UserName} s'est connect??");

            var role = await _userManager.GetRolesAsync(user);

            return Json(new
            {
                UserName = user.UserName,
                Role = role.FirstOrDefault(),
                ProfileImageUrl = user.ProfileImageUrl
            });
        }

        /// <summary>
        /// M??thode de connexion utilisant la double authentification.
        /// Prend en param??tre le mod??le de connexion, un bool??en et une cha??ne de caract??res.
        /// Selon le r??sultat d??livr?? par la requ??te de connexion, la m??thode renverra un r??sultat diff??rent.
        /// Si les identifiants sont corrects, elle renverra un JSON Web Token.
        /// Si l'utilisateur est banni de connexion, il sera renvoy?? ?? la m??thode associ??e.
        /// Dans tous les autres cas, la m??thode renverra un sch??ma JSON du mod??le pass?? en param??tre.
        /// </summary>
        /// <param name="model">Mod??le de connexion o?? les identifiants et le code de s??curit?? sont rentr??s</param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2FA(LoginWith2FAModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            // var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            // if (user == null)
            //     return Json(new { error = $"Impossible d'acc??der au pseudo '{_userManager.GetUserName(User)}'." });

            var user = await _userManager.FindByNameAsync(model.UserName);
            var ipAddress = HttpContext.GetRemoteIPAddress().ToString();
            var errors = await CheckUserCredentials(user, model.Password, ipAddress);

            if (!string.IsNullOrEmpty(errors))
                return NotFound(new { error = errors });

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            // var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, model.RememberMe, model.RememberMachine);
            var codeValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, model.TwoFactorCode);

            if (!codeValid)
                return BadRequest(new { error = "Code de s??curit?? incorrect." });

            // D??but de la co
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
                IssuedUtc = DateTime.UtcNow,
            };

            try
            {
                await _signInManager.SignInAsync(user, authProperties);
            }
            catch (Exception exception)
            {
                return BadRequest(new { errors = exception.InnerException });
            }
            // Fin de la co

            RefreshXsrfToken();
            _logger.LogInformation($"{user.UserName} s'est connect?? avec la 2FA");

            var role = await _userManager.GetRolesAsync(user);

            return Json(new
            {
                UserName = user.UserName,
                Role = role.FirstOrDefault(),
                ProfileImageUrl = user.ProfileImageUrl
            });
        }

        /// <summary>
        /// M??thode de connexion utilisant un code de v??rification.
        /// Prend en param??tre le mod??le de connexion, un bool??en et une cha??ne de caract??res.
        /// Selon le r??sultat d??livr?? par la requ??te de connexion, la m??thode renverra un r??sultat diff??rent.
        /// Si les identifiants sont corrects, elle renverra un JSON Web Token.
        /// Si l'utilisateur est banni de connexion, il sera renvoy?? ?? la m??thode associ??e.
        /// Dans tous les autres cas, la m??thode renverra un sch??ma JSON du mod??le pass?? en param??tre.
        /// </summary>
        /// <param name="model">Mod??le de connexion o?? le code de r??cup??ration est rentr??</param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            // var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            // if (user == null)
            //     return Json(new { error = $"Impossible d'acc??der au pseudo '{_userManager.GetUserName(User)}'." });

            var user = await _userManager.FindByNameAsync(model.UserName);
            var ipAddress = HttpContext.GetRemoteIPAddress().ToString();
            var errors = await CheckUserCredentials(user, model.Password, ipAddress);

            if (!string.IsNullOrEmpty(errors))
                return NotFound(new { error = errors });

            var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);
            // var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);
            var codeValid = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, model.RecoveryCode);

            if (!codeValid.Succeeded)
                return BadRequest(new { error = "Code de r??cup??ration incorrect" });

            // D??but de la co
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
                IssuedUtc = DateTime.UtcNow,
            };

            try
            {
                await _signInManager.SignInAsync(user, authProperties);
            }
            catch (Exception exception)
            {
                return BadRequest(new { errors = exception.InnerException });
            }
            // Fin de la co

            RefreshXsrfToken();
            _logger.LogInformation($"{user.UserName} s'est connect?? avec un code de r??cup??ration");

            var role = await _userManager.GetRolesAsync(user);

            return Json(new
            {
                UserName = user.UserName,
                Role = role.FirstOrDefault(),
                ProfileImageUrl = user.ProfileImageUrl
            });
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> IsLogged()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                var role = await _userManager.GetRolesAsync(user);

                return Json(new
                {
                    UserName = user.UserName,
                    Role = role.FirstOrDefault(),
                    ProfileImageUrl = user.ProfileImageUrl
                });
            }

            return Json(new { user = false });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity.Name;

            try
            {
                await _signInManager.SignOutAsync();
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = exception.InnerException });
            }
            finally
            {
                await HttpContext.SignOutAsync();
                _logger.LogInformation($"{userName} s'est d??connect??.");
            }

            return Ok();
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!await IsReCaptchValid(model.CaptchaResponse))
                return BadRequest(new { error = "Echec de v??rification du captcha" });

            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            var userEmail = await _userManager.FindByEmailAsync(model.Email);
            var userName = await _userManager.FindByNameAsync(model.UserName);
            var ipAddress = HttpContext.GetRemoteIPAddress().ToString();

            if (userEmail != null)
                return Json(new { error = "Un utilisateur avec cette adresse ??lectronique existe d??j??" });

            if (userName != null)
                return Json(new { error = "Un utilisateur avec ce pseudo existe d??j??" });

            if (await IsBlackListed(ipAddress))
                return Json(new { error = "Cette adresse IP est bannie" });

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                ProfileImageUrl = "/images/users/default.png",
                MemberSince = DateTime.UtcNow,
                IpAddress = HttpContext.GetRemoteIPAddress().ToString()
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            var role = await _userManager.AddToRoleAsync(user, "Member");

            if (result.Succeeded && role.Succeeded)
            {
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
                    return Json(new { error = $"Erreur lors de l'envoi de l'email : {exception.InnerException}" });
                }

                _logger.LogInformation($"Un nouveau compte {user.UserName} a ??t?? cr????.");
            }

            return Json(result);
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
                return BadRequest(new { error = "Votre identifiant ou le code n'est pas fourni" });

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{userId}'." });

            var codeConverted = HttpUtility.UrlDecode(code);
            var result = await _userManager.ConfirmEmailAsync(user, codeConverted);

            return Json(result.Succeeded ? "Votre email est valid??" : "Erreur");
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)) || (await _userManager.GetEmailAsync(user) != model.Email))
                return BadRequest(new { error = "Les informations entr??es sont incorrectes" });

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var codeConverted = Uri.EscapeDataString(code);
            var callbackUrl = "https://" + HttpContext.Request.Host + "/recuperer-mon-compte" + "/" + user.Id + "/" + codeConverted;

            try
            {
                await _emailSender.SendEmailAsync(model.Email, "R??initialisation du mot de passe",
                    $"Veuillez r??initialiser votre mot de passe en cliquant ici: {callbackUrl}");
            }
            catch (Exception exception)
            {
                return BadRequest(new { error = $"Erreur lors de l'envoi de l'email : {exception.InnerException}" });
            }

            _logger.LogInformation($"{user.UserName} a demand?? la r??initialisation de son mdp");

            return Json(new { success = "Un email de reinitialisation est envoy??" });
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorList = (from item in ModelState.Values
                                 from error in item.Errors
                                 select error.ErrorMessage).ToList();

                return Json(new { errorModel = errorList });
            }

            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
                return NotFound(new { error = $"Impossible de charger l'utilisateur d'identifiant : '{user.Id}'." });

            var codeConverted = HttpUtility.UrlDecode(model.Code);
            var result = await _userManager.ResetPasswordAsync(user, codeConverted, model.Password);

            return result.Succeeded ? Json("Le mot de passe est r??initialis??.") : Json(new { error = "Jeton invalide, veuillez r??it??rer la demande." });
        }

        /// <summary>
        /// Nous avons une adresse IP. Cr??ation d'une collection d'utilisateurs ayant cette adresse IP au moment de la cr??ation de compte.
        /// Si l'utilisateur est banni : alors vrai (il est sur liste noire)
        /// Si on ne trouve personne (VPN ou autre), on cherche parmis tous les sujets post??s avec cette adresse IP : cr??ation d'une collection de Post.
        /// Si un post a pour auteur un banni : alors vrai.
        /// M??me principe pour les r??ponses.
        /// </summary>
        /// <param name="ipAddress">cha??ne de caract??res repr??sentant l'adresse IP ?? v??rifier</param>
        /// <returns>True si l'adresse IP est bannie. False sinon</returns>
        private async Task<bool> IsBlackListed(string ipAddress)
        {
            var users = await _userService.GetByIpAddress(ipAddress);
            var posts = await _postService.GetPostsByIpAddress(ipAddress);
            var replies = await _replyService.GetRepliesByIpAddress(ipAddress);

            if (users.Any(user => user.LockoutEnd != null))
                return true;

            if (posts.Any(post => post.User.LockoutEnd != null))
                return true;

            if (replies.Any(reply => reply.User.LockoutEnd != null))
                return true;

            return false;
        }

        private async Task<bool> IsReCaptchValid(string captchaResponse)
        {
            var apiUrl = "https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}";
            var requestUri = string.Format(apiUrl, _options.SecretKey, captchaResponse);
            var httpClient = new HttpClient();
            var request = await httpClient.PostAsync(requestUri, null);
            var response = await request.Content.ReadAsStringAsync();
            var jResponse = JObject.Parse(response);
            var isSuccess = jResponse.Value<bool>("success");

            return isSuccess;
        }

        /// <summary>
        /// Renvoie une chaine de caract??res d??crivant l'erreur. Si aucune erreur n'est d??tect??e, la chaine sera vide
        /// </summary>
        /// <param name="user">L'utilisateur dont le mot de passe est ?? v??rifier</param>
        /// <param name="password">Le mot de passe ?? comparer avec celui hash??</param>
        /// <param name="ipAddress">L'adresse IP avec laquelle la connexion a lieu</param>
        /// <returns>chaine de caract??res d??crivant l'erreur</returns>
        private async Task<string> CheckUserCredentials(ApplicationUser user, string password, string ipAddress)
        {
            if (user == null)
                return "Cet utilisateur n'existe pas";

            if (user.LockoutEnd != null)
                return "Cet utilisateur est banni";

            if (!user.EmailConfirmed)
                return "Veuillez confirmer votre adresse e-mail";

            if (await IsBlackListed(ipAddress))
                return "Cette adresse IP est bannie";

            var passwordValid = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            var legacyPasswordValid = _accountService.VerifyLegacyPassword(password, user.PasswordHash);

            if (passwordValid == PasswordVerificationResult.Failed && legacyPasswordValid == false)
                return "Mot de passe incorrect";

            return string.Empty;
        }

        private void RefreshXsrfToken()
        {
            HttpContext.Response.Cookies.Delete("XSRF-TOKEN");

            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

            HttpContext.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions() { HttpOnly = false });
        }
    }
}