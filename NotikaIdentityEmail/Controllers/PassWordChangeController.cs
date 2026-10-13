using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MimeKit;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models.ForgetPasswordModels;
using System.Threading.Tasks;

namespace NotikaIdentityEmail.Controllers
{
    public class PassWordChangeController : Controller
    {
        private readonly    UserManager<AppUser> _userManager;

        public PassWordChangeController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordViewModel forgetPasswordViewModel)
        {
            var user = await _userManager.FindByEmailAsync(forgetPasswordViewModel.Email);

            string passwordResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            var passwordResetTokenLink = Url.Action("ResetPassword", "PasswordChange", new
            {
                userId = user.Id,
                token = passwordResetToken
            }, HttpContext.Request.Scheme);

            MimeMessage mimeMessage = new MimeMessage();

            var smtpEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL")
                ?? throw new InvalidOperationException("SMTP_EMAIL is not configured.");
            var smtpAppPassword = Environment.GetEnvironmentVariable("SMTP_APP_PASSWORD")
                ?? throw new InvalidOperationException("SMTP_APP_PASSWORD is not configured.");
            MailboxAddress mailboxAddressFrom = new MailboxAddress("Notika Admin", smtpEmail);
            mimeMessage.From.Add(mailboxAddressFrom);

            MailboxAddress mailboxAddressTo= new MailboxAddress("User", forgetPasswordViewModel.Email);
            mimeMessage.To.Add(mailboxAddressTo);

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.TextBody=passwordResetTokenLink;
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            mimeMessage.Subject = "Şifre Değişiklik Talebi";
            SmtpClient client = new SmtpClient();
            client.Connect("smtp.gmail.com", 587, false);
            client.Authenticate(smtpEmail, smtpAppPassword);
            client.Send(mimeMessage);
            client.Disconnect(true);


            return View();

        }

        [HttpGet]
        public IActionResult ResetPassword (string userId, string token)
        {
            TempData["userId"] = userId; TempData["token"]= token;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult>ResetPassword(ResetPassordViewModel resetPassordViewModel)
        {
            var userId = TempData["userId"];
            var token = TempData["token"];

            if (userId == null || token == null) 
            {
                ViewBag.v = "Hata Oluştu";
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (resetPassordViewModel.Password != resetPassordViewModel.ConfirmPassword) {
                ModelState.AddModelError("", "Şifreler Eşleşmiyor.");
                return View();
            }
            var result = await _userManager.ResetPasswordAsync(user, token.ToString(), resetPassordViewModel.Password);
            if (result.Succeeded) 
            {
                return RedirectToAction("UserLogin", "Login");
            }
            return View();

        }
    }
}
