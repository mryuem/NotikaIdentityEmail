using Microsoft.AspNetCore.Mvc;

namespace NotikaIdentityEmail.ViewComponents
{
    public class _FooterUserLayoutComponenetPartial:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
