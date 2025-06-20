using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace SimpleShop.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; } // 範例自訂欄位
        public virtual ICollection<Order>? Orders { get; set; }
    }
}
