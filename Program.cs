using System.Linq;
using Microsoft.EntityFrameworkCore;
using MiniShare.Data;
using MiniShare.Models;
using MiniShare.Services;
using Microsoft.AspNetCore.Identity;
using Pomelo.EntityFrameworkCore.MySql;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSession();

// 注册文件上传服务
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// DbContext + MySQL
var connectionString = builder.Configuration.GetConnectionString("MiniShareDb");
builder.Services.AddDbContext<MiniShare.Data.ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 29))));

// Identity (int keys)
builder.Services.AddIdentity<MiniShare.Models.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<int>>(options =>
{
    // 放宽密码/账号限制
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 1;
    options.Password.RequiredUniqueChars = 1;
    options.User.RequireUniqueEmail = true; // 恢复邮箱唯一性
})
    .AddEntityFrameworkStores<MiniShare.Data.ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// 配置Cookie认证选项以支持保持登录状态
builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie设置
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // 30天保持登录
    options.LoginPath = "/Login/Index";
    options.LogoutPath = "/Login/Logout";
    options.AccessDeniedPath = "/Login/Index";
    options.SlidingExpiration = true; // 滑动过期：每次请求时重置过期时间
});

var app = builder.Build();

// 初始化管理员账号
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedAdminAsync(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

static async Task SeedAdminAsync(IServiceProvider services)
{
    var userManager = services.GetRequiredService<UserManager<MiniShare.Models.ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<Microsoft.AspNetCore.Identity.IdentityRole<int>>>();

    const string adminRoleName = "Admin";
    const string adminEmail = "admin@admin.com";
    const string adminPassword = "Admin123";

    if (!await roleManager.RoleExistsAsync(adminRoleName))
    {
        var role = new Microsoft.AspNetCore.Identity.IdentityRole<int>
        {
            Name = adminRoleName,
            NormalizedName = adminRoleName.ToUpperInvariant()
        };
        await roleManager.CreateAsync(role);
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new MiniShare.Models.ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            DisplayName = "Administrator"
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            throw new Exception("创建管理员账号失败: " + string.Join(";", createResult.Errors.Select(e => e.Description)));
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
    {
        await userManager.AddToRoleAsync(adminUser, adminRoleName);
    }
}
