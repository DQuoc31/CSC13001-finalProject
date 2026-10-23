using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ danh mục sản phẩm kèm số lượng máy.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Lấy chi tiết danh mục theo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null)
            return NotFound(new { message = $"Không tìm thấy danh mục có ID = {id}." });

        return Ok(category);
    }

    /// <summary>
    /// Thêm mới một danh mục sản phẩm (Admin, Manager).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var result = await _categoryService.CreateAsync(dto);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Cập nhật thông tin danh mục.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { message = "ID trong URL không trùng khớp với dữ liệu gửi lên." });

        var result = await _categoryService.UpdateAsync(dto);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Xóa danh mục sản phẩm (chỉ Admin).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteAsync(id);
        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }
}
