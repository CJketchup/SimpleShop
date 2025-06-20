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


// *************************** �����ˬd�I ***************************
// �T�O AddDefaultIdentity ���x���ѼƬO ApplicationUser
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    // �z�� Identity �ﶵ�A�Ҧp�G
    options.SignIn.RequireConfirmedAccount = false; // ²��_���A�������l��T�{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6; // ²��K�X
})
    .AddRoles<IdentityRole>() // �p�G�z�ϥΤF����\��
    .AddEntityFrameworkStores<ApplicationDbContext>(); // ���w�z�� DbContext
// *****************************************************************


builder.Services.AddControllersWithViews();

// Session �A��
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HttpContextAccessor (ShoppingCart �ݭn)
builder.Services.AddHttpContextAccessor();

// ShoppingCart
builder.Services.AddScoped<ShoppingCart>(sp => ShoppingCart.GetCart(sp));

// EmailSender (�ϥ� Identity �� IEmailSender ���f)
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

// �������ҩM���v�����n��
app.UseAuthentication(); // �����b UseAuthorization ���e
app.UseAuthorization();

app.UseSession(); // �ҥ� Session

// ���Ѱt�m
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Identity UI (�p�n�J����) �ݭn

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // �T�O�o�̽ШD���O UserManager<ApplicationUser>
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
// �b app.Run() ���e�K�[���եN�X
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var testUserManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        Console.WriteLine("Successfully resolved UserManager<ApplicationUser>."); // �Ϊ̨ϥ� ILogger
        var testUserManagerWrongType = services.GetService<UserManager<IdentityUser>>(); // �`�N�O GetService�A�p�G�S���|��^ null
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
        Console.WriteLine($"Error resolving UserManager for test: {ex.Message}"); // �Ϊ̨ϥ� ILogger
        throw; // �����ε{���b�o�̱Y��A�H�K�ݨ�̰l��
    }
}
// Seed Data ...
app.Run();