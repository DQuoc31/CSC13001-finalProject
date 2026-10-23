using LaptopStore.Application.DTOs.Reports;
using LaptopStore.Application.Interfaces;
using LaptopStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Application.Services;

public class ReportService : IReportService
{
    private readonly IApplicationDbContext _context;

    public ReportService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var completedOrders = await _context.Orders
            .Where(o => o.IsActive && o.Status == OrderStatus.Completed)
            .ToListAsync();

        var thisMonth = DateTime.UtcNow.Month;
        var thisYear = DateTime.UtcNow.Year;

        var totalRevenue = completedOrders.Sum(o => o.FinalAmount);
        var monthRevenue = completedOrders
            .Where(o => o.CreatedAt.Month == thisMonth && o.CreatedAt.Year == thisYear)
            .Sum(o => o.FinalAmount);

        var totalOrders = await _context.Orders.CountAsync(o => o.IsActive);
        var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
        var lowStock = await _context.Products.CountAsync(p => p.IsActive && p.StockQuantity <= p.MinStockLevel);
        var totalCustomers = await _context.Customers.CountAsync(c => c.IsActive);

        return new DashboardSummaryDto
        {
            TotalRevenue = totalRevenue,
            MonthRevenue = monthRevenue,
            TotalOrders = totalOrders,
            CompletedOrders = completedOrders.Count,
            TotalProducts = totalProducts,
            LowStockProducts = lowStock,
            TotalCustomers = totalCustomers
        };
    }

    public async Task<List<RevenueReportDto>> GetRevenueByMonthAsync(int year)
    {
        var orders = await _context.Orders
            .Where(o => o.IsActive && o.Status == OrderStatus.Completed && o.CreatedAt.Year == year)
            .ToListAsync();

        var result = new List<RevenueReportDto>();
        for (int m = 1; m <= 12; m++)
        {
            var monthOrders = orders.Where(o => o.CreatedAt.Month == m).ToList();
            result.Add(new RevenueReportDto
            {
                Period = $"Tháng {m}/{year}",
                Revenue = monthOrders.Sum(o => o.FinalAmount),
                OrderCount = monthOrders.Count
            });
        }

        return result;
    }

    public async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(int top = 5)
    {
        var completedOrderIds = await _context.Orders
            .Where(o => o.IsActive && o.Status == OrderStatus.Completed)
            .Select(o => o.Id)
            .ToListAsync();

        var items = await _context.OrderItems
            .Where(item => completedOrderIds.Contains(item.OrderId))
            .ToListAsync();

        var list = items
            .GroupBy(item => new { item.ProductId, item.ProductName })
            .Select(g => new TopSellingProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                Brand = "",
                TotalQuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.SubTotal)
            })
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(top)
            .ToList();

        foreach (var item in list)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (product != null)
            {
                item.Brand = product.Brand;
            }
        }

        return list;
    }

    public async Task<List<LowStockProductDto>> GetLowStockProductsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new LowStockProductDto
            {
                ProductId = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Brand = p.Brand,
                StockQuantity = p.StockQuantity,
                MinStockLevel = p.MinStockLevel
            })
            .ToListAsync();
    }
}
