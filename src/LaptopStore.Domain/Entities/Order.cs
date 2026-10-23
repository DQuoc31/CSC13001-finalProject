using LaptopStore.Domain.Common;
using LaptopStore.Domain.Enums;

namespace LaptopStore.Domain.Entities;

/// <summary>
/// Đơn hàng bán tại cửa hàng hoặc online.
/// </summary>
public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty; // HD-2026-0001...
    
    // Khách hàng
    public int CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    // Nhân viên tạo đơn
    public int? CreatedByUserId { get; set; }
    public virtual User? CreatedByUser { get; set; }

    // Tài chính
    public decimal TotalAmount { get; set; }        // Tổng tiền hàng
    public decimal DiscountAmount { get; set; } = 0;// Chiết khấu / Giảm giá
    public decimal FinalAmount { get; set; }        // Thành tiền sau giảm giá

    // Trạng thái & Phương thức
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    // Địa chỉ & Ghi chú
    public string? ShippingAddress { get; set; }
    public string? CustomerNote { get; set; }
    public string? InternalNote { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Danh sách mặt hàng
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
