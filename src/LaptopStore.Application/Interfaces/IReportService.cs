using LaptopStore.Application.DTOs.Reports;

namespace LaptopStore.Application.Interfaces;

public interface IReportService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<List<RevenueReportDto>> GetRevenueByMonthAsync(int year);
    Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(int top = 5);
    Task<List<LowStockProductDto>> GetLowStockProductsAsync();
}
