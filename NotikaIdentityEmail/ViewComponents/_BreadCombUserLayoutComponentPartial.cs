
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.ViewComponents
{
    public class _BreadCombUserLayoutComponentPartial : ViewComponent
    {
        private readonly UserManager<AppUser> _userManager;

        public _BreadCombUserLayoutComponentPartial(
            UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                return Content("Kullanıcı bulunamadı.");
            }

            return View(user);
        }
    }
}
