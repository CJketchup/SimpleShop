// 檔案：SimpleShop/Controllers/OrderController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleShop.Data;
using SimpleShop.Models;         // 確保 Order, OrderItem, ApplicationUser, OrderCheckoutViewModel 在這裡
using SimpleShop.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleShop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ShoppingCart _shoppingCart;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        // private readonly ILogger<OrderController> _logger; // 如果需要日誌

        public OrderController(ApplicationDbContext context,
                               ShoppingCart shoppingCart,
                               UserManager<ApplicationUser> userManager,
                               IEmailSender emailSender /*, ILogger<OrderController> logger */)
        {
            _context = context;
            _shoppingCart = shoppingCart;
            _userManager = userManager;
            _emailSender = emailSender;
            // _logger = logger;
        }

        // GET: /Order/Checkout
        public IActionResult Checkout()
        {
            if (!_shoppingCart.Items.Any())
            {
                TempData["ErrorMessage"] = "您的購物車是空的。";
                return RedirectToAction("Index", "Cart");
            }
            ViewBag.Total = _shoppingCart.GetTotal();
            return View(new OrderCheckoutViewModel()); // <<--- 傳遞新的 ViewModel
        }

        // POST: /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(OrderCheckoutViewModel viewModel) // <<--- 接收新的 ViewModel
        {
            if (!_shoppingCart.Items.Any())
            {
                ModelState.AddModelError("", "您的購物車是空的。");
                // 即使模型有效，如果購物車為空，也應該阻止。
                // 或者在 Action 開始時就檢查購物車。
            }

            // 再次檢查庫存
            // (這部分邏輯不變，因為它依賴 _shoppingCart 和 _context)
            foreach (var item in _shoppingCart.Items)
            {
                var productInDb = await _context.Products.FindAsync(item.ProductId);
                if (productInDb == null || productInDb.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError("", $"商品 '{item.ProductName}' 庫存不足或已下架。");
                }
            }

            // 現在 ModelState.IsValid 只會驗證 OrderCheckoutViewModel 中的屬性 (例如 ShippingAddress)
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    // 理論上 [Authorize] 會處理，但以防萬一
                    return Challenge(); // 或者 RedirectToPage("/Account/Login", new { area = "Identity" })
                }

                // 創建一個新的 Order 實體來保存到資料庫
                var newDbOrder = new Order
                {
                    UserId = currentUser.Id, // 從登入用戶獲取
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = _shoppingCart.GetTotal(), // 從購物車獲取
                    Status = OrderStatus.Pending,
                    ShippingAddress = viewModel.ShippingAddress, // <<--- 從 viewModel 獲取地址
                    // 如果 viewModel 中有 Notes:
                    // Notes = viewModel.Notes,
                    OrderItems = new List<OrderItem>()
                };

                // 填充 OrderItems 並扣減庫存 (這部分邏輯不變)
                foreach (var cartItem in _shoppingCart.Items)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product != null && product.StockQuantity >= cartItem.Quantity)
                    {
                        product.StockQuantity -= cartItem.Quantity;
                        _context.Update(product);

                        newDbOrder.OrderItems.Add(new OrderItem
                        {
                            ProductId = cartItem.ProductId,
                            Quantity = cartItem.Quantity,
                            PriceAtPurchase = cartItem.Price // 假設 CartItem 中有 Price
                        });
                    }
                    else
                    {
                        // 這種情況應該在上面的庫存檢查中被捕獲到 ModelState.AddModelError
                        // 但以防萬一，這裡做一個最終處理
                        TempData["ErrorMessage"] = $"處理訂單時發現 '{cartItem.ProductName}' 庫存不足。請返回購物車修改。";
                        // 可能需要將用戶導向購物車頁面，並保留他們已填寫的地址信息 (可以通過 TempData 傳遞)
                        // ViewBag.Total = _shoppingCart.GetTotal();
                        // return View(viewModel); // 返回 Checkout 頁面並顯示錯誤
                        return RedirectToAction("Index", "Cart"); // 或者直接返回購物車
                    }
                }

                _context.Orders.Add(newDbOrder);
                await _context.SaveChangesAsync();

                _shoppingCart.ClearCart();

                // 發送訂單確認郵件給客戶 (這部分邏輯不變)
                if (currentUser != null && !string.IsNullOrEmpty(currentUser.Email))
                {
                    string subject = $"您的 SimpleShop 訂單 #{newDbOrder.Id} 已確認";
                    string messageBody = $"親愛的 {currentUser.FullName ?? currentUser.UserName}，<br><br>" +
                                         $"感謝您的訂購！您的訂單編號 <strong>{newDbOrder.Id}</strong> 已成功建立。<br>" +
                                         $"訂單總金額為：{newDbOrder.TotalAmount:C}<br>" +
                                         $"我們將在商品準備好出貨時再次通知您。<br><br>" +
                                         $"SimpleShop 團隊";
                    try
                    {
                        await _emailSender.SendEmailAsync(currentUser.Email, subject, messageBody);
                        TempData["SuccessMessage"] = $"訂單已成功建立！確認郵件已發送到 {currentUser.Email}。";
                    }
                    catch (Exception ex)
                    {
                        // _logger.LogError(ex, "Failed to send order confirmation email for order {OrderId}", newDbOrder.Id);
                        TempData["SuccessMessage"] = "訂單已成功建立！但發送確認郵件失敗。";
                        TempData["ErrorMessage"] = $"郵件發送錯誤: {ex.Message}";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = "訂單已成功建立！";
                }

                return RedirectToAction("Confirmation", new { id = newDbOrder.Id });
            }

            // 如果 ModelState (針對 viewModel) 無效，則返回 Checkout View 以顯示驗證錯誤
            // 例如，如果 ShippingAddress 未填寫
            ViewBag.Total = _shoppingCart.GetTotal();
            return View(viewModel); // <<--- 將 viewModel (包含用戶已填寫的地址) 返回給 View
        }

        // ... Confirmation 和 MyOrders 方法保持不變 ...
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                                    .Include(o => o.User)
                                    .Include(o => o.OrderItems)
                                    .ThenInclude(oi => oi.Product)
                                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders
                                    .Where(o => o.UserId == userId)
                                    .Include(o => o.OrderItems)
                                    .ThenInclude(oi => oi.Product)
                                    .OrderByDescending(o => o.OrderDate)
                                    .ToListAsync();
            return View(orders);
        }
    }
}