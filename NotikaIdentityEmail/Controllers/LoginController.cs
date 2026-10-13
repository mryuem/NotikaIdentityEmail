
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models.IdentityModels;
using NotikaIdentityEmail.Models.JwtModels;
using NuGet.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NotikaIdentityEmail.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly JwtSettingsModel _jwtSettingsModel;

        public LoginController(
            SignInManager<AppUser> signInManager,
            EmailContext context,
            UserManager<AppUser> userManager,
            IOptions<JwtSettingsModel> jwtSettingsModel)
        {
            _signInManager = signInManager;
            _context = context;
            _userManager = userManager;
            _jwtSettingsModel = jwtSettingsModel.Value;
        }


        // NORMAL LOGIN SAYFASI
        [HttpGet]
        public IActionResult UserLogin()
        {
            return View();
        }


        // NORMAL LOGIN
        [HttpPost]
        public async Task<IActionResult> UserLogin(UserLoginViewModel model)
        {

            var value = _context.Users
                .FirstOrDefault(x => x.UserName == model.Username);

            SimpleUserViewModel simpleUserViewModel = new SimpleUserViewModel()
            {
                Id = value.Id,
                Name = value.Name,
                Surname = value.Surname,
                City = value.City,
                Username = value.UserName,
                Email = value.Email
            };

            if (value == null)
            {
                ModelState.AddModelError("", "Kullanıcı bulunamadı.");
                return View(model);
            }

            if (!value.EmailConfirmed)
            {
                ModelState.AddModelError(
                    "",
                    "Email hesabınız henüz onaylanmamıştır. Lütfen hesabınızı onaylayın!!!");

                return View(model);
            }
            if (!value.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Kullanıcı pasif durumda, giriş yapamaz!");

                return View(model);
            }



            var result = await _signInManager.PasswordSignInAsync(
                model.Username,
                model.Password,
                true,
                true);

            if (result.Succeeded)
            {
                var token = GenerateJwtToken(simpleUserViewModel);
                Response.Cookies.Append("jwtToken", token,new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(_jwtSettingsModel.ExpireMinutes)
                });

                return RedirectToAction("EditProfile", "Profile");
            }

            ModelState.AddModelError(
                "",
                "Hatalı kullanıcı adı ya da şifre girdiniz!");

            return View(model);
        }

        public string GenerateJwtToken(SimpleUserViewModel simpleUserViewModel)
        {
            var claim = new[]
            {
                new Claim("name",simpleUserViewModel.Name),
                new Claim("surname",simpleUserViewModel.Surname),
                new Claim("city",simpleUserViewModel.City),
                new Claim("username",simpleUserViewModel.Username),
                new Claim(ClaimTypes.NameIdentifier,simpleUserViewModel.Id),
                new Claim(ClaimTypes.Email,simpleUserViewModel.Email),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettingsModel.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettingsModel.Issuer,
                audience: _jwtSettingsModel.Audience,
                claims: claim,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettingsModel.ExpireMinutes),
                signingCredentials: creds);

            //simpleUserViewModel.Token = new JwtSecurityTokenHandler().WriteToken(token);
            //return View(simpleUserViewModel);
            return new JwtSecurityTokenHandler().WriteToken(token); 
        }


        // ÇIKIŞ YAP
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            Response.Cookies.Delete("jwtToken");
            return RedirectToAction("UserLogin", "Login");
        }


        // GOOGLE'A GİT
        [HttpPost]
        public IActionResult ExternalLogin(
            string provider,
            string? returnUrl = null)
        {
            var redirectUrl = Url.Action(
                "ExternalLoginCallBack",
                "Login",
                new { returnUrl });

            var properties =
                _signInManager.ConfigureExternalAuthenticationProperties(
                    provider,
                    redirectUrl);

            return Challenge(properties, provider);
        }


        // GOOGLE'DAN GERİ DÖNÜŞ
        // BURASI GET OLMALI
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallBack(
            string? returnUrl = null,
            string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");


            // Google hata verdiyse
            if (remoteError != null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Google giriş hatası: {remoteError}");

                return RedirectToAction("UserLogin");
            }


            // Google'dan kullanıcı bilgilerini al
            var info =
                await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                return RedirectToAction("UserLogin");
            }


            // Kullanıcı daha önce Google ile giriş yapmış mı?
            var result =
                await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider,
                    info.ProviderKey,
                    isPersistent: false);


            // Daha önce Google ile giriş yaptıysa
            if (result.Succeeded)
            {
                return RedirectToAction("Inbox", "Message");
            }


            // Google email adresini al
            var email =
                info.Principal.FindFirstValue(ClaimTypes.Email);


            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(
                    "",
                    "Google hesabından email adresi alınamadı.");

                return RedirectToAction("UserLogin");
            }


            // Yeni kullanıcı oluştur
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,

                Name = info.Principal
                    .FindFirstValue(ClaimTypes.GivenName)
                    ?? "Google",

                Surname = info.Principal
                    .FindFirstValue(ClaimTypes.Surname)
                    ?? "User"
            };


            var identityResult =
                await _userManager.CreateAsync(user);


            if (identityResult.Succeeded)
            {
                // Google hesabını kullanıcıya bağla
                await _userManager.AddLoginAsync(
                    user,
                    info);


                // Kullanıcıyı giriş yaptır
                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false);


                return RedirectToAction(
                    "Inbox",
                    "Message");
            }


            // Kullanıcı oluşturulamadıysa
            foreach (var error in identityResult.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            return RedirectToAction("UserLogin");
        }


    }
}


