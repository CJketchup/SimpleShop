using SimpleShop.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleShop.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public DateTime OrderDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public OrderStatus Status { get; set; }


        [Required(ErrorMessage = "請填寫收貨地址")] // 添加 ErrorMessage 以便前端顯示
        public string ShippingAddress { get; set; } = string.Empty;

        public string? TrackingNumber { get; set; } // 出貨追蹤號碼
        public DateTime? ShippedDate { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}