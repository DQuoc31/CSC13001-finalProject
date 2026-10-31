using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.Interfaces;
using LaptopStore.Domain.Common;
using LaptopStore.Domain.Entities;
using LaptopStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Application.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;

    public OrderService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderDto>> GetAllAsync(OrderStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.CreatedByUser)
            .Include(o => o.OrderItems)
            .Where(o => o.IsActive)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= toDate.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        var o = await _context.Orders
            .Include(x => x.Customer)
            .Include(x => x.CreatedByUser)
            .Include(x => x.OrderItems)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        return o != null ? MapToDto(o) : null;
    }

    public async Task<OrderDto?> GetByOrderNumberAsync(string orderNumber)
    {
        var o = await _context.Orders
            .Include(x => x.Customer)
            .Include(x => x.CreatedByUser)
            .Include(x => x.OrderItems)
            .FirstOrDefaultAsync(x => x.OrderNumber.ToLower() == orderNumber.Trim().ToLower() && x.IsActive);

        return o != null ? MapToDto(o) : null;
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderDto dto, int? userId = null)
    {
        // 1. Kiểm tra danh sách mặt hàng
        if (dto.Items == null || dto.Items.Count == 0)
            return Result<OrderDto>.Failure("Đơn hàng phải có ít nhất một sản phẩm.");

        // 2. Kiểm tra khách hàng tồn tại
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.IsActive);
        if (customer == null)
            return Result<OrderDto>.Failure("Khách hàng không tồn tại trong hệ thống.");

        // 3. Kiểm tra sản phẩm và tồn kho
        decimal calculatedTotal = 0;
        var orderItems = new List<OrderItem>();
        var inventoryTransactions = new List<InventoryTransaction>();

        var orderCountToday = await _context.Orders.CountAsync(o => o.CreatedAt.Date == DateTime.UtcNow.Date);
        var orderNumber = $"HD{DateTime.UtcNow:yyyyMMdd}-{orderCountToday + 1:D4}";

        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Quantity <= 0)
                return Result<OrderDto>.Failure("Số lượng mua phải lớn hơn 0.");

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemDto.ProductId && p.IsActive);
            if (product == null)
                return Result<OrderDto>.Failure($"Sản phẩm với ID {itemDto.ProductId} không tồn tại.");

            if (product.StockQuantity < itemDto.Quantity)
                return Result<OrderDto>.Failure($"Sản phẩm '{product.Name}' chỉ còn {product.StockQuantity} sản phẩm trong kho, không đủ đáp ứng số lượng {itemDto.Quantity}.");

            var lineTotal = (product.Price * itemDto.Quantity) - itemDto.Discount;
            if (lineTotal < 0) lineTotal = 0;
            calculatedTotal += lineTotal;

            // Trừ tồn kho sản phẩm
            product.StockQuantity -= itemDto.Quantity;
            if (product.StockQuantity == 0)
            {
                product.Status = ProductStatus.OutOfStock;
            }
            product.UpdatedAt = DateTime.UtcNow;

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSku = product.Sku,
                Quantity = itemDto.Quantity,
                UnitPrice = product.Price,
                Discount = itemDto.Discount
            };
            orderItems.Add(orderItem);

            // Ghi nhận biến động kho
            inventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                QuantityChange = -itemDto.Quantity,
                BalanceAfter = product.StockQuantity,
                Type = StockTransactionType.SaleDeduction,
                ReferenceDocument = orderNumber,
                Note = $"Xuất bán theo đơn hàng {orderNumber}",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (dto.DiscountAmount < 0)
            return Result<OrderDto>.Failure("Chiết khấu đơn hàng không được nhỏ hơn 0.");

        var finalAmount = calculatedTotal - dto.DiscountAmount;
        if (finalAmount < 0) finalAmount = 0;

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customer.Id,
            CreatedByUserId = userId,
            TotalAmount = calculatedTotal,
            DiscountAmount = dto.DiscountAmount,
            FinalAmount = finalAmount,
            Status = OrderStatus.Pending,
            PaymentMethod = dto.PaymentMethod,
            PaymentStatus = dto.PaymentMethod == PaymentMethod.Cash ? PaymentStatus.Paid : PaymentStatus.Unpaid,
            ShippingAddress = dto.ShippingAddress ?? customer.Address,
            CustomerNote = dto.CustomerNote,
            InternalNote = dto.InternalNote,
            OrderItems = orderItems,
            CreatedAt = DateTime.UtcNow
        };

        // Cập nhật tổng chi tiêu của khách hàng
        customer.TotalSpent += finalAmount;
        customer.RewardPoints += (int)(finalAmount / 100000); // 100k = 1 điểm thưởng
        customer.UpdatedAt = DateTime.UtcNow;

        _context.Orders.Add(order);
        foreach (var tx in inventoryTransactions)
        {
            _context.InventoryTransactions.Add(tx);
        }

        await _context.SaveChangesAsync();

        return Result<OrderDto>.Success(MapToDto(order));
    }

    public async Task<Result> UpdateStatusAsync(int orderId, UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.IsActive);
        if (order == null)
            return Result.Failure("Đơn hàng không tồn tại.");

        if (order.Status == OrderStatus.Completed && dto.Status != OrderStatus.Completed)
            return Result.Failure("Đơn hàng đã hoàn thành không thể đổi trạng thái.");

        if (order.Status == OrderStatus.Cancelled)
            return Result.Failure("Đơn hàng đã bị hủy, không thể đổi trạng thái.");

        // Hủy đơn phải đi qua CancelOrderAsync để hoàn kho và trừ lại điểm thưởng
        if (dto.Status == OrderStatus.Cancelled)
            return Result.Failure("Vui lòng dùng chức năng Hủy đơn hàng để hủy đơn.");

        order.Status = dto.Status;
        if (dto.PaymentStatus.HasValue)
        {
            order.PaymentStatus = dto.PaymentStatus.Value;
        }

        if (dto.Status == OrderStatus.Completed)
        {
            order.CompletedAt = DateTime.UtcNow;
            order.PaymentStatus = PaymentStatus.Paid;
        }

        if (!string.IsNullOrWhiteSpace(dto.Note))
        {
            order.InternalNote = string.IsNullOrEmpty(order.InternalNote)
                ? dto.Note
                : $"{order.InternalNote} | {dto.Note}";
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> CancelOrderAsync(int orderId, string reason)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.IsActive);

        if (order == null)
            return Result.Failure("Đơn hàng không tồn tại.");

        if (order.Status == OrderStatus.Completed)
            return Result.Failure("Không thể hủy đơn hàng đã hoàn tất giao dịch.");

        if (order.Status == OrderStatus.Cancelled)
            return Result.Failure("Đơn hàng này đã bị hủy trước đó.");

        order.Status = OrderStatus.Cancelled;
        order.InternalNote = string.IsNullOrEmpty(order.InternalNote)
            ? $"Lý do hủy: {reason}"
            : $"{order.InternalNote} | Lý do hủy: {reason}";
        order.UpdatedAt = DateTime.UtcNow;

        // Trừ lại tổng chi tiêu và điểm thưởng đã cộng khi lập đơn
        if (order.Customer != null)
        {
            order.Customer.TotalSpent = Math.Max(0, order.Customer.TotalSpent - order.FinalAmount);
            order.Customer.RewardPoints = Math.Max(0, order.Customer.RewardPoints - (int)(order.FinalAmount / 100000));
            order.Customer.UpdatedAt = DateTime.UtcNow;
        }

        // Hoàn lại số lượng tồn kho cho các sản phẩm trong đơn
        foreach (var item in order.OrderItems)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (product != null)
            {
                product.StockQuantity += item.Quantity;
                if (product.Status == ProductStatus.OutOfStock && product.StockQuantity > 0)
                {
                    product.Status = ProductStatus.Active;
                }
                product.UpdatedAt = DateTime.UtcNow;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    QuantityChange = item.Quantity,
                    BalanceAfter = product.StockQuantity,
                    Type = StockTransactionType.CustomerReturn,
                    ReferenceDocument = order.OrderNumber,
                    Note = $"Hoàn kho do hủy đơn hàng {order.OrderNumber}",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        return Result.Success();
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer?.FullName ?? "Khách lẻ",
        CustomerPhone = o.Customer?.PhoneNumber ?? string.Empty,
        CreatedByUserId = o.CreatedByUserId,
        CreatedByUserName = o.CreatedByUser?.FullName,
        TotalAmount = o.TotalAmount,
        DiscountAmount = o.DiscountAmount,
        FinalAmount = o.FinalAmount,
        Status = o.Status,
        PaymentMethod = o.PaymentMethod,
        PaymentStatus = o.PaymentStatus,
        ShippingAddress = o.ShippingAddress,
        CustomerNote = o.CustomerNote,
        CreatedAt = o.CreatedAt,
        CompletedAt = o.CompletedAt,
        Items = o.OrderItems.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ProductSku = i.ProductSku,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Discount = i.Discount,
            SubTotal = i.SubTotal
        }).ToList()
    };
}
