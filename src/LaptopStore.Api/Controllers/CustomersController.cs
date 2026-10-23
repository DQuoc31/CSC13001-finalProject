using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Lấy danh sách khách hàng, hỗ trợ tìm kiếm theo tên/SĐT.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,SalesStaff")]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var customers = await _customerService.GetAllAsync(search);
        return Ok(customers);
    }

    /// <summary>
    /// Lấy chi tiết khách hàng theo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Manager,SalesStaff")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
            return NotFound(new { message = $"Không tìm thấy khách hàng có ID = {id}." });

        return Ok(customer);
    }

    /// <summary>
    /// Lấy lịch sử đơn hàng của một khách hàng.
    /// </summary>
    [HttpGet("{id:int}/orders")]
    [Authorize(Roles = "Admin,Manager,SalesStaff")]
    public async Task<IActionResult> GetCustomerOrders(int id)
    {
        var orders = await _customerService.GetCustomerOrdersAsync(id);
        return Ok(orders);
    }

    /// <summary>
    /// Thêm mới khách hàng.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,SalesStaff")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        var result = await _customerService.CreateAsync(dto);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Cập nhật thông tin khách hàng.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager,SalesStaff")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { message = "ID trong URL không trùng khớp với dữ liệu gửi lên." });

        var result = await _customerService.UpdateAsync(dto);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Xóa khách hàng (chỉ Admin).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _customerService.DeleteAsync(id);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }
}
