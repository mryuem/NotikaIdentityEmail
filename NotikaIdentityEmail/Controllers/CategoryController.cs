using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace NotikaIdentityEmail.Controllers
{
    public class CategoryController : Controller
    {

        private readonly EmailContext _context;

        public CategoryController(EmailContext context)
        {
            _context = context;
        }

        public IActionResult CategoryList()
        {
            var token = Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Giriş Yapmalısınız...";
                return RedirectToAction("UserLogin", "Login");
            }

            JwtSecurityToken jwt;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                jwt = handler.ReadJwtToken(token);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Geçersiz Token. Lütfen tekrar giriş yapın.";
                return RedirectToAction("UserLogin", "Login");
            }

            var city=jwt.Claims.FirstOrDefault(c=> c.Type == "city")?.Value;
            if (city != "İstanbul")
            {
                return Forbid();
            }


                var values = _context.Categories.ToList();
            return View(values);
        }
        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View();
        }
        [HttpPost]
        public IActionResult CreateCategory(Category category)
        {
            category.CategoryStatus = true;
            _context.Categories.Add(category);
            _context.SaveChanges();
            return RedirectToAction("CategoryList");
        }
        public IActionResult DeleteCategory(int id)
        {
            var value = _context.Categories.Find(id);
            _context.Categories.Remove(value);
            _context.SaveChanges();
            return RedirectToAction("CategoryList");
        }

        public IActionResult UpdateCategory(int id) { 
        
            var value = _context.Categories.Find(id);
            return View(value);
        }
        [HttpPost]
        public IActionResult UpdateCategory(Category category) { 
        
            var value = _context.Categories.Update(category);
            _context.SaveChanges();
            return RedirectToAction("CategoryList");
        }


    }
}
