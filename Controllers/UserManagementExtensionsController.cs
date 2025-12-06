using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace MiniShare.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("[controller]")]
    public class UserManagementExtensionsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserManagementExtensionsController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // 用户管理扩展主页面
        public IActionResult Index()
        {
            return View();
        }

        // 批量操作用户角色
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("BatchUpdateRoles")]
        public async Task<IActionResult> BatchUpdateRoles(BatchUpdateRolesViewModel model)
        {
            if (ModelState.IsValid)
            {
                foreach (var userId in model.UserIds)
                {
                    var user = await _userManager.FindByIdAsync(userId.ToString());
                    if (user != null)
                    {
                        // 先移除所有角色
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        
                        // 添加新角色
                        await _userManager.AddToRolesAsync(user, model.RolesToAdd);
                    }
                }
                return Json(new { success = true, message = "批量更新角色成功！" });
            }
            return Json(new { success = false, message = "模型验证失败" });
        }

        // 获取用户统计信息
        [Route("UserStatistics")]
        public async Task<IActionResult> UserStatistics()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var regularUsers = await _userManager.GetUsersInRoleAsync("User");
            
            var stats = new UserStatisticsViewModel
            {
                TotalUsers = totalUsers,
                AdminCount = adminUsers.Count,
                UserCount = regularUsers.Count,
                PendingApprovals = 0 // 可以根据实际需求添加待审批用户数量
            };
            
            return Json(stats);
        }

        // 搜索用户
        [HttpGet]
        [Route("SearchUsers")]
        public async Task<IActionResult> SearchUsers(string searchTerm)
        {
            var query = _userManager.Users.AsQueryable();
            
            // 如果有搜索条件，才添加搜索过滤
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }
            
            var users = await query
                .Take(20)
                .Select(u => new { u.Id, u.UserName, u.Email })
                .ToListAsync();
            
            return Json(users);
        }

        // 导出用户列表
        [HttpGet]
        [Route("ExportUsers")]
        public async Task<IActionResult> ExportUsers()
        {
            var users = await _userManager.Users
                .Include(u => _context.UserRoles.Where(ur => ur.UserId == u.Id))
                .ThenInclude(ur => _context.Roles.Where(r => r.Id == ur.RoleId))
                .ToListAsync();
            
            // 简单的CSV导出示例
            var csv = "ID,用户名,邮箱,角色\n";
            
            foreach (var user in users)
            {
                var roles = string.Join(", ", await _userManager.GetRolesAsync(user));
                csv += $"{user.Id},{user.UserName},{user.Email},{roles}\n";
            }
            
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "用户列表.csv");
        }

        // 锁定/解锁用户
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("ToggleUserLockout")]
        public async Task<IActionResult> ToggleUserLockout(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Json(new { success = false, message = "用户不存在" });
            }
            
            if (await _userManager.IsLockedOutAsync(user))
            {
                // 解锁用户
                await _userManager.SetLockoutEndDateAsync(user, null);
                return Json(new { success = true, message = "用户已解锁", isLocked = false });
            }
            else
            {
                // 锁定用户
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(1));
                return Json(new { success = true, message = "用户已锁定", isLocked = true });
            }
        }
    }

    // 批量更新角色视图模型
    public class BatchUpdateRolesViewModel
    {
        [Required(ErrorMessage = "必须选择至少一个用户")]
        public string[] UserIds { get; set; }
        
        [Required(ErrorMessage = "必须选择至少一个角色")]
        public string[] RolesToAdd { get; set; }
    }

    // 用户统计信息视图模型
    public class UserStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int AdminCount { get; set; }
        public int UserCount { get; set; }
        public int PendingApprovals { get; set; }
    }
}