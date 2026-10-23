using LaptopStore.Domain.Common;

namespace LaptopStore.Domain.Entities;

/// <summary>
/// Chi tiết dòng sản phẩm trong đơn hàng.
/// </summary>
public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public virtual Order? Order { get; set; }

    public int ProductId { get; set; }
    public virtual Product? Product { get; set; }

    // Lưu snapshot thông tin sản phẩm tại thời điểm mua (để khi giá thay đổi sau này không bị lệch hóa đơn cũ)
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal SubTotal => (UnitPrice * Quantity) - Discount;
}
