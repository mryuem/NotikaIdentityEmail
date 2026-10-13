using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using System.Threading.Tasks;

namespace NotikaIdentityEmail.ViewComponents
{
    public class _HeaderLayoutComponentPartial:ViewComponent
    {
        private readonly EmailContext _emailContext;
        private readonly UserManager<AppUser> _userManager;
        public _HeaderLayoutComponentPartial(EmailContext emailContext, UserManager<AppUser> userManager)
        {
            _emailContext = emailContext;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated || string.IsNullOrEmpty(User.Identity.Name))
            {
                ViewBag.UserEmailCount = 0;
                ViewBag.notificationCount = 0;
                return View();
            }

            var userValue = await _userManager.FindByNameAsync(User.Identity.Name);
            if (userValue == null)
            {
                ViewBag.UserEmailCount = 0;
                ViewBag.notificationCount = 0;
                return View();
            }

            var userEmail = userValue.Email;
            var userEmailCount=_emailContext.Messages.Where(x=>x.ReceiverEmail==userEmail).Count(); 
            ViewBag.UserEmailCount = userEmailCount;
            ViewBag.notificationCount = _emailContext.Notifications.Count();
            return View();
        }
    }
}
