using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Domain.Common;
using LaptopStore.Domain.Enums;

namespace LaptopStore.Application.Interfaces;

public interface IOrderService
{
    Task<List<OrderDto>> GetAllAsync(OrderStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<OrderDto?> GetByIdAsync(int id);
    Task<OrderDto?> GetByOrderNumberAsync(string orderNumber);
    Task<Result<OrderDto>> CreateOrderAsync(CreateOrderDto dto, int? userId = null);
    Task<Result> UpdateStatusAsync(int orderId, UpdateOrderStatusDto dto);
    Task<Result> CancelOrderAsync(int orderId, string reason);
}
