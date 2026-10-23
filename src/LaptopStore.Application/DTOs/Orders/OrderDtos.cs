using LaptopStore.Domain.Enums;

namespace LaptopStore.Application.DTOs.Orders;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? ShippingAddress { get; set; }
    public string? CustomerNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal SubTotal { get; set; }
}

public class CreateOrderDto
{
    public int CustomerId { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? ShippingAddress { get; set; }
    public string? CustomerNote { get; set; }
    public string? InternalNote { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; } = 0;
}

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? Note { get; set; }
}
