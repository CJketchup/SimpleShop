using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SimpleShop.Models; // 確保 ApplicationUser, Product, Order, OrderItem 模型在這裡

namespace SimpleShop.Data
{
    // 關鍵點 1: IdentityDbContext 的泛型參數
    // 第一個參數必須是您的自訂用戶類 ApplicationUser
    // 第二個參數是 IdentityRole (如果使用角色)
    // 第三個參數是主鍵類型 (通常是 string)
    // 後續的是 Identity 提供的其他相關實體類型，通常保持預設即可
    public class ApplicationDbContext : IdentityDbContext<
        ApplicationUser,      // 您的自訂用戶類
        IdentityRole,         // Identity 角色類
        string,               // 主鍵類型
        IdentityUserClaim<string>,
        IdentityUserRole<string>,
        IdentityUserLogin<string>,
        IdentityRoleClaim<string>,
        IdentityUserToken<string>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 您的自訂 DbSet
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // 必須先調用 base.OnModelCreating(builder)
            // 這會執行 Identity 框架自身的模型配置 (例如設置表名、主鍵等)
            base.OnModelCreating(builder);

            // 針對 SQLite 調整 Identity 表格中可能導致 nvarchar(max) 問題的欄位
            // SQLite 不支援 (max) 語法，長文本應使用 TEXT 類型

            // ApplicationUser (您的自訂用戶類，繼承自 IdentityUser)
            // IdentityUser 的屬性配置會在下面的 builder.Entity<IdentityUser> 中處理
            // 如果 ApplicationUser 有自己獨特的 string 屬性，且可能很長，可以在這裡配置
            builder.Entity<ApplicationUser>(b =>
            {
                // 範例：如果 FullName 允許非常長 (雖然通常不需要 TEXT)
                // b.Property(u => u.FullName).HasColumnType("TEXT");

                // ApplicationUser 上的 Orders 集合由 Order 實體的 Fluent API 配置
            });


            // IdentityUser (所有用戶的基類，包括 ApplicationUser)
            // 針對那些 Identity 預設可能被推斷為 (max) 的 string 屬性
            builder.Entity<IdentityUser>(b => // 也可以寫成 builder.Entity<IdentityUser<string>>()
            {
                b.Property(u => u.ConcurrencyStamp).HasColumnType("TEXT").IsConcurrencyToken();
                b.Property(u => u.SecurityStamp).HasColumnType("TEXT");
                b.Property(u => u.PhoneNumber).HasColumnType("TEXT"); // 如果您允許非常長的電話號碼或用於其他目的
                // 注意: Email, UserName 等通常有 MaxLength 限制，EF Core 預設會處理好
            });

            // IdentityRole
            builder.Entity<IdentityRole>(b => // 也可以寫成 builder.Entity<IdentityRole<string>>()
            {
                b.Property(r => r.ConcurrencyStamp).HasColumnType("TEXT").IsConcurrencyToken();
                // 注意: Name, NormalizedName 通常有 MaxLength 限制
            });

            // IdentityUserClaim<string>
            builder.Entity<IdentityUserClaim<string>>(b =>
            {
                // ClaimType 和 ClaimValue 可能包含任意長度的字串
                b.Property(uc => uc.ClaimType).HasColumnType("TEXT");
                b.Property(uc => uc.ClaimValue).HasColumnType("TEXT");
            });

            // IdentityUserLogin<string>
            // LoginProvider 和 ProviderKey 通常有固定的長度，並且是主鍵的一部分
            // EF Core 應該能正確處理，但如果遇到問題，可以明確指定 MaxLength
            builder.Entity<IdentityUserLogin<string>>(b =>
            {
                // 預設情況下，這些鍵通常會有 MaxLength 約束，對 SQLite 來說會轉為 TEXT
                // 但如果遇到 (max) 錯誤，可以這樣明確指定，EF Core for SQLite 會處理
                b.Property(ul => ul.LoginProvider).HasMaxLength(128);
                b.Property(ul => ul.ProviderKey).HasMaxLength(128);
            });

            // IdentityRoleClaim<string>
            builder.Entity<IdentityRoleClaim<string>>(b =>
            {
                b.Property(rc => rc.ClaimType).HasColumnType("TEXT");
                b.Property(rc => rc.ClaimValue).HasColumnType("TEXT");
            });

            // IdentityUserToken<string>
            // LoginProvider 和 Name 通常有固定的長度
            // Value (Token 值) 可能非常長
            builder.Entity<IdentityUserToken<string>>(b =>
            {
                b.Property(ut => ut.LoginProvider).HasMaxLength(128);
                b.Property(ut => ut.Name).HasMaxLength(128);
                b.Property(ut => ut.Value).HasColumnType("TEXT");
            });


            // --- 您自訂實體的配置 ---

            // Product
            builder.Entity<Product>(b =>
            {
                b.Property(p => p.Price)
                   .HasColumnType("decimal(18, 2)"); // SQLite 會將 decimal 映射為 REAL 或 TEXT
                                                     // EF Core 會處理運行時的轉換
                                                     // decimal 類型在 SQLite 中沒有原生支持，但 EF Core 可模擬
            });


            // Order
            builder.Entity<Order>(b =>
            {
                b.Property(o => o.TotalAmount)
                   .HasColumnType("decimal(18, 2)");

                // 配置 Order 與 ApplicationUser (一對多關係)
                // ApplicationUser 有一個 ICollection<Order> Orders
                // Order 有一個 UserId 和一個 ApplicationUser User 導航屬性
                b.HasOne(o => o.User)
                 .WithMany(u => u.Orders) // 指向 ApplicationUser.Orders 屬性
                 .HasForeignKey(o => o.UserId)
                 .IsRequired(); // 通常訂單必須有關聯用戶
            });


            // OrderItem
            builder.Entity<OrderItem>(b =>
            {
                b.Property(oi => oi.PriceAtPurchase)
                   .HasColumnType("decimal(18, 2)");

                // OrderItem 與 Order (多對一)
                // OrderItem 與 Product (多對一，但 Product 端可能沒有集合導航屬性)
                // EF Core 可以推斷這些關係，但明確配置更好
            });

            // 如果有其他需要針對 SQLite 進行調整的欄位類型或約束，可以在此處添加
        }
    }
}