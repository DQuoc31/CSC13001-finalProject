using LaptopStore.Application.DTOs.Products;
using LaptopStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaptopStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Tìm kiếm và lọc danh sách sản phẩm.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter)
    {
        var products = await _productService.GetAllAsync(filter);
        return Ok(products);
    }

    /// <summary>
    /// Lấy chi tiết sản phẩm theo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return NotFound(new { message = $"Không tìm thấy sản phẩm có ID = {id}." });

        return Ok(product);
    }

    /// <summary>
    /// Lấy chi tiết sản phẩm theo mã SKU.
    /// </summary>
    [HttpGet("sku/{sku}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySku(string sku)
    {
        var product = await _productService.GetBySkuAsync(sku);
        if (product == null)
            return NotFound(new { message = $"Không tìm thấy sản phẩm có SKU '{sku}'." });

        return Ok(product);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm sắp hết hàng (tồn kho dưới hoặc bằng ngưỡng tối thiểu).
    /// </summary>
    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
    public async Task<IActionResult> GetLowStock()
    {
        var products = await _productService.GetLowStockAsync();
        return Ok(products);
    }

    /// <summary>
    /// Thêm mới một sản phẩm vào hệ thống.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var result = await _productService.CreateAsync(dto);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Cập nhật thông tin sản phẩm.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { message = "ID trong URL không trùng khớp với dữ liệu gửi lên." });

        var result = await _productService.UpdateAsync(dto);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Xóa mềm một sản phẩm khỏi hệ thống (chỉ Admin).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.DeleteAsync(id);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Điều chỉnh số lượng tồn kho của sản phẩm (nhập thêm hoặc kiểm kê điều chỉnh).
    /// </summary>
    [HttpPost("{id:int}/adjust-stock")]
    [Authorize(Roles = "Admin,WarehouseStaff")]
    public async Task<IActionResult> AdjustStock(int id, [FromBody] AdjustStockRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(userIdStr, out var u) ? u : null;

        var result = await _productService.AdjustStockAsync(id, request.QuantityChange, request.Note, userId);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Cập nhật tồn kho thành công." });
    }
}

public class AdjustStockRequest
{
    public int QuantityChange { get; set; }
    public string Note { get; set; } = string.Empty;
}
