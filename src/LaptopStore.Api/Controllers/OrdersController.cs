using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.Interfaces;
using LaptopStore.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaptopStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ đơn hàng (có thể lọc theo trạng thái và khoảng ngày).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,SalesStaff,WarehouseStaff")]
    public async Task<IActionResult> GetAll(
        [FromQuery] OrderStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var orders = await _orderService.GetAllAsync(status, fromDate, toDate);
        return Ok(orders);
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng theo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Manager,SalesStaff,WarehouseStaff")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null)
            return NotFound(new { message = $"Không tìm thấy đơn hàng có ID = {id}." });

        return Ok(order);
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng theo số hóa đơn (OrderNumber).
    /// </summary>
    [HttpGet("number/{orderNumber}")]
    [Authorize(Roles = "Admin,Manager,SalesStaff,WarehouseStaff")]
    public async Task<IActionResult> GetByOrderNumber(string orderNumber)
    {
        var order = await _orderService.GetByOrderNumberAsync(orderNumber);
        if (order == null)
            return NotFound(new { message = $"Không tìm thấy đơn hàng với mã '{orderNumber}'." });

        return Ok(order);
    }

    /// <summary>
    /// Lập đơn hàng mới tại quầy hoặc online.
    /// Xử lý trừ tồn kho, tính tổng tiền và ghi nhận giao dịch.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,SalesStaff")]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(userIdStr, out var u) ? u : null;

        var result = await _orderService.CreateOrderAsync(dto, userId);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng (Đang xử lý, Giao hàng, Hoàn thành).
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin,Manager,SalesStaff,WarehouseStaff")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var result = await _orderService.UpdateStatusAsync(id, dto);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Cập nhật trạng thái đơn hàng thành công." });
    }

    /// <summary>
    /// Hủy đơn hàng và tự động hoàn lại số lượng tồn kho (Admin, Manager).
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelOrderRequest request)
    {
        var result = await _orderService.CancelOrderAsync(id, request.Reason);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Hủy đơn hàng thành công và đã hoàn trả tồn kho." });
    }
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}
