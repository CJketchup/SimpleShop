// 檔案：SimpleShop/Models/OrderCheckoutViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace SimpleShop.Models
{
    public class OrderCheckoutViewModel
    {
        [Required(ErrorMessage = "請填寫收貨地址")]
        [Display(Name = "收貨地址")] // 為了 <label asp-for="..."> 顯示中文
        public string ShippingAddress { get; set; } = string.Empty;

        // 如果您還需要在結帳頁面從用戶收集其他信息，可以在這裡添加屬性
        // 例如：
        // [Display(Name = "備註")]
        // public string? Notes { get; set; }
    }
}