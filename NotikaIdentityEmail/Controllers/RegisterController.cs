using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models.IdentityModels;

namespace NotikaIdentityEmail.Controllers
{
    public class RegisterController : Controller
    {

        private readonly UserManager<AppUser> _userManager;

        public RegisterController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateUser(RegisterViewModel model)
        {
            var smtpEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL");
            var smtpAppPassword = Environment.GetEnvironmentVariable("SMTP_APP_PASSWORD");
            if (string.IsNullOrWhiteSpace(smtpEmail) || string.IsNullOrWhiteSpace(smtpAppPassword))
            {
                ModelState.AddModelError("", "Kayıt e-postası için SMTP_EMAIL ve SMTP_APP_PASSWORD ayarlanmalıdır.");
                return View(model);
            }

            Random rnd = new Random();
            int code=rnd.Next(100000,1000000);
            AppUser appUser = new AppUser()
            {
                Name = model.Name,
                Email = model.Email,
                Surname = model.Surname,
                UserName = model.Username,
                ActivationCode = code,
            };

            var result = await _userManager.CreateAsync(appUser, model.Password);

            if (result.Succeeded)
            {
               MimeMessage mimeMessage = new MimeMessage();
                MailboxAddress mailboxAddressFrom = new MailboxAddress("Admin", smtpEmail);
                mimeMessage.From.Add(mailboxAddressFrom);

                MailboxAddress mailboxAddressTo=new MailboxAddress("User", model.Email);
                mimeMessage.To.Add(mailboxAddressTo);

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.TextBody = "Hesabınızı doğrulamak için gerekli olan aktivasyon kodu: " + code;
                mimeMessage.Body = bodyBuilder.ToMessageBody();

                SmtpClient client = new SmtpClient();
                client.Connect("smtp.gmail.com", 587, false);
                client.Authenticate(smtpEmail, smtpAppPassword);
                client.Send(mimeMessage);

                client.Disconnect(true);

                TempData["EmailMove"] = model.Email;

                return RedirectToAction("UserActivation", "Activation");

            }
            else
            {
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }

                return View();
            }

        }
    }
}
