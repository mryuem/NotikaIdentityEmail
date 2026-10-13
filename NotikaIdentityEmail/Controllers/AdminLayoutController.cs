using Microsoft.AspNetCore.Mvc;

namespace NotikaIdentityEmail.Controllers
{
    public class AdminLayoutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
