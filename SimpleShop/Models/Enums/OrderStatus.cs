namespace SimpleShop.Models.Enums
{
    public enum OrderStatus
    {
        Pending,        // 待處理
        Processing,     // 處理中
        Shipped,        // 已出貨
        Delivered,      // 已送達
        Cancelled       // 已取消
    }
}