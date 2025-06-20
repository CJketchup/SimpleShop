using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace SimpleShop.Models
{
    public class ShoppingCart
    {
        private const string CartSessionKey = "ShoppingCart";
        private readonly ISession _session;

        public List<CartItem> Items { get; private set; }

        private ShoppingCart(ISession session)
        {
            _session = session;
            Items = GetCartFromSession();
        }

        public static ShoppingCart GetCart(IServiceProvider services)
        {
            ISession? session = services.GetRequiredService<IHttpContextAccessor>()?.HttpContext?.Session;
            if (session == null)
            {
                throw new InvalidOperationException("Session has not been configured.");
            }
            return new ShoppingCart(session);
        }

        private List<CartItem> GetCartFromSession()
        {
            var cartJson = _session.GetString(CartSessionKey);
            return cartJson == null ? new List<CartItem>() : JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCartToSession()
        {
            _session.SetString(CartSessionKey, JsonConvert.SerializeObject(Items));
        }

        public void AddItem(Product product, int quantity)
        {
            var cartItem = Items.FirstOrDefault(item => item.ProductId == product.Id);
            if (cartItem == null)
            {
                if (quantity <= product.StockQuantity)
                {
                    Items.Add(new CartItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = quantity,
                        ImageUrl = product.ImageUrl
                    });
                }
                // 可以加上庫存不足的提示或例外處理
            }
            else
            {
                if (cartItem.Quantity + quantity <= product.StockQuantity)
                {
                    cartItem.Quantity += quantity;
                }
                // 可以加上庫存不足的提示或例外處理
            }
            SaveCartToSession();
        }

        public void RemoveItem(int productId)
        {
            var cartItem = Items.FirstOrDefault(item => item.ProductId == productId);
            if (cartItem != null)
            {
                Items.Remove(cartItem);
                SaveCartToSession();
            }
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var cartItem = Items.FirstOrDefault(item => item.ProductId == productId);
            if (cartItem != null)
            {
                // 這裡應加入檢查庫存的邏輯，此處簡化
                if (quantity > 0)
                {
                    cartItem.Quantity = quantity;
                }
                else
                {
                    Items.Remove(cartItem);
                }
                SaveCartToSession();
            }
        }

        public void ClearCart()
        {
            Items.Clear();
            SaveCartToSession();
        }

        public decimal GetTotal()
        {
            return Items.Sum(item => item.SubTotal);
        }
    }
}