using LaptopStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Thống kê tổng quan Dashboard (Doanh thu, số lượng đơn hàng, số khách hàng, tồn kho).
    /// Yêu cầu vai trò: Admin, Manager.
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var summary = await _reportService.GetDashboardSummaryAsync();
        return Ok(summary);
    }

    /// <summary>
    /// Thống kê doanh thu theo 12 tháng trong năm.
    /// Yêu cầu vai trò: Admin, Manager.
    /// </summary>
    [HttpGet("revenue")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetRevenueByYear([FromQuery] int? year)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var reports = await _reportService.GetRevenueByMonthAsync(targetYear);
        return Ok(reports);
    }

    /// <summary>
    /// Danh sách top sản phẩm bán chạy nhất.
    /// Yêu cầu vai trò: Admin, Manager.
    /// </summary>
    [HttpGet("top-selling")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetTopSelling([FromQuery] int top = 5)
    {
        var products = await _reportService.GetTopSellingProductsAsync(top);
        return Ok(products);
    }

    /// <summary>
    /// Danh sách sản phẩm tồn kho thấp cần nhập hàng.
    /// Yêu cầu vai trò: Admin, Manager, WarehouseStaff.
    /// </summary>
    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
    public async Task<IActionResult> GetLowStock()
    {
        var products = await _reportService.GetLowStockProductsAsync();
        return Ok(products);
    }
}
