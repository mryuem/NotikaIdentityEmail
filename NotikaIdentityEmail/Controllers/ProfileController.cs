using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models.IdentityModels;

namespace NotikaIdentityEmail.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public ProfileController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }
        [HttpGet]
        public async  Task<IActionResult> EditProfile()
        {
            var values = await _userManager.FindByNameAsync(User.Identity.Name);
            UserEditViewModel userEdit = new UserEditViewModel();
            userEdit.Name = values.Name;
            userEdit.Surname = values.Surname;
            userEdit.PhoneNumber = values.PhoneNumber;
            userEdit.ImageUrl = values.ImageUrl;
            userEdit.City = values.City;
            userEdit.UserName = values.UserName;
            userEdit.Email = values.Email;
            
            return View(userEdit);
        }
        [HttpPost]
        public async Task<IActionResult> EditProfile(UserEditViewModel model)
        {
            if (model.Password == model.PasswordConfirm) 
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                user.Name = model.Name;
                user.Surname = model.Surname;
                user.PhoneNumber = model.PhoneNumber;
                user.City = model.City;
                user.UserName = model.UserName;
                user.Email = model.Email;
                user.ImageUrl= model.ImageUrl;
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, model.Password);
                await _userManager.UpdateAsync(user);
            }
            return View();
        }
    }
}
