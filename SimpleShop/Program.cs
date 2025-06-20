using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // For IEmailSender if you are using Identity's one
using Microsoft.EntityFrameworkCore;
using SimpleShop.Data;
using SimpleShop.Models;
using SimpleShop.Services; // Your EmailSender implementation

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();


// *************************** 關鍵檢查點 ***************************
// 確保 AddDefaultIdentity 的泛型參數是 ApplicationUser
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    // 您的 Identity 選項，例如：
    options.SignIn.RequireConfirmedAccount = false; // 簡單起見，先關閉郵件確認
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6; // 簡單密碼
})
    .AddRoles<IdentityRole>() // 如果您使用了角色功能
    .AddEntityFrameworkStores<ApplicationDbContext>(); // 指定您的 DbContext
// *****************************************************************


builder.Services.AddControllersWithViews();

// Session 服務
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HttpContextAccessor (ShoppingCart 需要)
builder.Services.AddHttpContextAccessor();

// ShoppingCart
builder.Services.AddScoped<ShoppingCart>(sp => ShoppingCart.GetCart(sp));

// EmailSender (使用 Identity 的 IEmailSender 接口)
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, SimpleShop.Services.EmailSender>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 身份驗證和授權中介軟體
app.UseAuthentication(); // 必須在 UseAuthorization 之前
app.UseAuthorization();

app.UseSession(); // 啟用 Session

// 路由配置
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Identity UI (如登入頁面) 需要

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // 確保這裡請求的是 UserManager<ApplicationUser>
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        await SeedData.Initialize(services, userManager, roleManager, context, configuration);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}
// 在 app.Run() 之前添加測試代碼
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var testUserManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        Console.WriteLine("Successfully resolved UserManager<ApplicationUser>."); // 或者使用 ILogger
        var testUserManagerWrongType = services.GetService<UserManager<IdentityUser>>(); // 注意是 GetService，如果沒有會返回 null
        if (testUserManagerWrongType != null)
        {
            Console.WriteLine("Surprisingly, UserManager<IdentityUser> is also registered.");
        }
        else
        {
            Console.WriteLine("UserManager<IdentityUser> is NOT registered (which is expected).");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error resolving UserManager for test: {ex.Message}"); // 或者使用 ILogger
        throw; // 讓應用程式在這裡崩潰，以便看到棧追蹤
    }
}
// Seed Data ...
app.Run();