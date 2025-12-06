using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniShare.Models;

namespace MiniShare.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(string userName, string? avatarUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            user.UserName = userName;
            user.AvatarUrl = avatarUrl;
            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Profile));
        }

        [AllowAnonymous]
        public IActionResult Auth()
        {
            return Redirect("/Login");
        }
    }
}