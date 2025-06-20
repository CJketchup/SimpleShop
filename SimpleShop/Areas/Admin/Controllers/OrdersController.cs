using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleShop.Data;
using SimpleShop.Models;
using SimpleShop.Models.Enums;
using SimpleShop.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services; // <--- 這是關鍵的 using 語句

namespace SimpleShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public OrdersController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                                    .Include(o => o.User) // 載入關聯的 User
                                    .OrderByDescending(o => o.OrderDate)
                                    .ToListAsync();
            return View(orders);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }


        // POST: Admin/Orders/ShipOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipOrder(int id, string trackingNumber)
        {
            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Processing)
            {
                order.Status = OrderStatus.Shipped;
                order.ShippedDate = DateTime.UtcNow;
                order.TrackingNumber = trackingNumber;
                _context.Update(order);
                await _context.SaveChangesAsync();

                // 發送出貨通知郵件
                if (order.User != null && !string.IsNullOrEmpty(order.User.Email))
                {
                    string subject = $"您的訂單 #{order.Id} 已出貨";
                    string message = $"親愛的顧客，<br><br>" +
                                     $"您的訂單編號 {order.Id} 已於 {order.ShippedDate:yyyy-MM-dd HH:mm} 出貨。<br>" +
                                     $"{(string.IsNullOrWhiteSpace(trackingNumber) ? "您的包裹正在路上。" : $"您的貨運追蹤號碼是：{trackingNumber}")}<br><br>" +
                                     $"感謝您的訂購！<br><br>" +
                                     $"SimpleShop 團隊";
                    await _emailSender.SendEmailAsync(order.User.Email, subject, message);
                }
                TempData["SuccessMessage"] = $"訂單 #{order.Id} 已標記為已出貨。";
            }
            else
            {
                TempData["ErrorMessage"] = $"訂單 #{order.Id} 的狀態為 {order.Status}，無法設定為已出貨。";
            }

            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
    }
}