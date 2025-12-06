using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MiniShare.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        // 用户列表
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var thisViewModel = new UserRolesViewModel();
                thisViewModel.UserId = user.Id;
                thisViewModel.Email = user.Email;
                thisViewModel.UserName = user.UserName;
                thisViewModel.Roles = await GetUserRoles(user);
                userRolesViewModel.Add(thisViewModel);
            }

            return View(userRolesViewModel);
        }

        // 获取用户角色
        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }

        // 编辑用户
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            var model = new EditUserViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Bio = user.Bio,
                Gender = user.Gender,
                Birthday = user.Birthday,
                Major = user.Major,
                School = user.School,
                AvatarUrl = user.AvatarUrl,
                SelectedRoles = userRoles.ToArray(),
                AllRoles = allRoles.Select(r => r.Name).ToArray()
            };

            return View(model);
        }

        // 保存用户编辑
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // 更新用户信息
                user.UserName = model.UserName;
                user.Email = model.Email;
                user.Bio = model.Bio;
                user.Gender = model.Gender;
                user.Birthday = model.Birthday;
                user.Major = model.Major;
                user.School = model.School;
                user.AvatarUrl = model.AvatarUrl;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    // 更新用户角色
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var rolesToAdd = model.SelectedRoles.Except(currentRoles);
                    var rolesToRemove = currentRoles.Except(model.SelectedRoles);

                    await _userManager.AddToRolesAsync(user, rolesToAdd);
                    await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            var allRoles = await _roleManager.Roles.ToListAsync();
            model.AllRoles = allRoles.Select(r => r.Name).ToArray();
            return View(model);
        }

        // 修改密码
        public async Task<IActionResult> ChangePassword(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel { UserId = user.Id };
            return View(model);
        }

        // 保存新密码
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    // 如果是当前用户修改自己的密码，需要重新登录
                    if (User.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id.ToString())
                    {
                        await _signInManager.RefreshSignInAsync(user);
                    }

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // 删除用户
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            // 禁止删除当前登录用户
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id.ToString())
            {
                TempData["ErrorMessage"] = "不能删除当前登录的用户！";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "删除用户失败：" + string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }
    }

    // ViewModels
    public class UserRolesViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
    }

    public class EditUserViewModel
    {
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "用户名不能为空")]
        public string UserName { get; set; }
        
        [Required(ErrorMessage = "邮箱不能为空")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        public string Email { get; set; }
        
        public string? Bio { get; set; }
        
        public Gender Gender { get; set; }
        
        public DateTime? Birthday { get; set; }
        
        public string? Major { get; set; }
        
        public string? School { get; set; }
        
        public string? AvatarUrl { get; set; }
        
        public string[] SelectedRoles { get; set; }
        public string[] AllRoles { get; set; }
    }

    public class ChangePasswordViewModel
    {
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(100, ErrorMessage = "{0} 必须至少包含 {2} 个字符。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "新密码")]
        public string NewPassword { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "确认新密码")]
        [Compare("NewPassword", ErrorMessage = "新密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; }
    }
}