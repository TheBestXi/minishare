using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace MiniShare.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public UserProfileController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        /// <summary>
        /// 显示用户资料
        /// </summary>
        /// <returns>用户资料视图</returns>
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        /// <summary>
        /// 编辑用户资料页面
        /// </summary>
        /// <returns>编辑页面</returns>
        public async Task<IActionResult> Edit()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        /// <summary>
        /// 保存用户资料
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <param name="avatarFile">头像文件</param>
        /// <returns>重定向到资料页面</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser user, IFormFile? avatarFile)
        {
            var userId = GetCurrentUserId();
            if (user.Id != userId)
            {
                return Forbid();
            }

            // 验证用户名唯一性
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == user.UserName && u.Id != userId);
            if (existingUser != null)
            {
                ModelState.AddModelError("UserName", "用户名已被使用");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 从数据库获取当前用户信息
                    var currentUser = await _context.Users.FindAsync(userId);
                    if (currentUser == null)
                    {
                        return NotFound();
                    }

                    // 保存原始的 Email 和 NormalizedEmail（这些不应该被修改）
                    var originalEmail = currentUser.Email;
                    var originalNormalizedEmail = currentUser.NormalizedEmail;
                    var originalNormalizedUserName = currentUser.NormalizedUserName;

                    // 处理头像上传
                    if (avatarFile != null)
                    {
                        // 验证文件类型
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                        var extension = Path.GetExtension(avatarFile.FileName).ToLower();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("avatarFile", "只允许上传 JPG 或 PNG 格式的图片");
                            return View(user);
                        }

                        // 验证文件大小（最大 2MB）
                        if (avatarFile.Length > 2 * 1024 * 1024)
                        {
                            ModelState.AddModelError("avatarFile", "头像大小不能超过 2MB");
                            return View(user);
                        }

                        // 保存文件
                        var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads/avatars");
                        Directory.CreateDirectory(uploadsFolder);
                        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await avatarFile.CopyToAsync(fileStream);
                        }

                        // 更新头像URL
                        currentUser.AvatarUrl = $"/uploads/avatars/{uniqueFileName}";
                    }

                    // 只更新允许修改的属性
                    currentUser.UserName = user.UserName;
                    currentUser.Bio = user.Bio;
                    currentUser.Gender = user.Gender;
                    currentUser.Birthday = user.Birthday;
                    currentUser.Major = user.Major;
                    currentUser.School = user.School;

                    // 确保 Email 和 NormalizedEmail 保持不变
                    currentUser.Email = originalEmail;
                    currentUser.NormalizedEmail = originalNormalizedEmail;
                    // 更新 NormalizedUserName（根据新的 UserName 生成）
                    currentUser.NormalizedUserName = user.UserName.ToUpperInvariant();

                    // 保存更新
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(user);
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        /// <returns>当前用户ID</returns>
        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                throw new UnauthorizedAccessException();
            }
            return userId;
        }
    }
}