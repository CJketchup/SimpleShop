using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleShop.Models;
using SimpleShop.Models.Enums; // For OrderStatus

namespace SimpleShop.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider,
                                            UserManager<ApplicationUser> userManager,
                                            RoleManager<IdentityRole> roleManager,
                                            ApplicationDbContext context,
                                            IConfiguration configuration)
        {
            // 檢查資料庫是否已遷移
            context.Database.Migrate(); // 自動應用掛起的遷移

            // 1. Seed Roles
            string[] roleNames = { "Admin", "Customer" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Admin User
            string adminEmail = configuration["AdminEmail"] ?? "admin@example.com";
            string adminPassword = configuration["AdminPassword"] ?? "Password123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true, // 直接確認
                    FullName = "Administrator"
                };
                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Products (if none exist)
            if (!context.Products.Any())
            {
                context.Products.AddRange(
                    new Product
                    {
                        Name = "機械鍵盤",
                        Description = "青軸手感，電競與程式設計師的最愛。",
                        Price = 2500.00m,
                        StockQuantity = 30,
                        ImageUrl = "/images/keyboard.jpg" // 假設圖片放在 wwwroot/images/
                    },
                    new Product
                    {
                        Name = "無線滑鼠",
                        Description = "人體工學設計，長時間使用依然舒適。",
                        Price = 799.00m,
                        StockQuantity = 75,
                        ImageUrl = "/images/mouse.jpg"
                    },
                    new Product
                    {
                        Name = "賽車椅",
                        Description = "長時間使用不易出汗，碳鋼支架承受強大的操作體驗",
                        Price = 22988.00m,
                        StockQuantity = 10,
                        ImageUrl = "/images/playseat_trophy.jpg"

                    },
                    new Product
                    {
                        Name = "精美筆記本",
                        Description = "高品質書寫用紙，適合各種筆類。",
                        Price = 150.00m,
                        StockQuantity = 100,
                        ImageUrl = "/images/notebook.jpg" 
                    },
                    new Product
                    {
                        Name = "專業鋼筆",
                        Description = "流暢書寫體驗，商務人士首選。",
                        Price = 1200.00m,
                        StockQuantity = 50,
                        ImageUrl = "/images/fountain_pen.jpg"
                    }
                    
                    
                );
                await context.SaveChangesAsync();
            }
        }
    }
}