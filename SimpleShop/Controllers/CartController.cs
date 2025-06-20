using Microsoft.AspNetCore.Mvc;
using SimpleShop.Data;
using SimpleShop.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // For [Authorize] if checkout needs login

namespace SimpleShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ShoppingCart _shoppingCart;

        public CartController(ApplicationDbContext context, ShoppingCart shoppingCart)
        {
            _context = context;
            _shoppingCart = shoppingCart;
        }

        public IActionResult Index()
        {
            ViewBag.Total = _shoppingCart.GetTotal();
            return View(_shoppingCart.Items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            if (product.StockQuantity < quantity)
            {
                TempData["ErrorMessage"] = $"商品 '{product.Name}' 庫存不足 (剩餘 {product.StockQuantity} 件)。";
                return RedirectToAction("Index", "Home"); // 或商品詳情頁
            }

            _shoppingCart.AddItem(product, quantity);
            TempData["SuccessMessage"] = $"已將 '{product.Name}' 加入購物車。";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int productId)
        {
            _shoppingCart.RemoveItem(productId);
            TempData["SuccessMessage"] = "已從購物車移除商品。";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            if (quantity > product.StockQuantity)
            {
                TempData["ErrorMessage"] = $"商品 '{product.Name}' 庫存不足 (剩餘 {product.StockQuantity} 件)，無法更新數量至 {quantity}。";
                // 將數量設為最大庫存
                _shoppingCart.UpdateQuantity(productId, product.StockQuantity);
            }
            else if (quantity <= 0)
            {
                _shoppingCart.RemoveItem(productId);
            }
            else
            {
                _shoppingCart.UpdateQuantity(productId, quantity);
            }
            return RedirectToAction("Index");
        }
    }
}