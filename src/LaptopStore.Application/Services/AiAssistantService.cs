using LaptopStore.Application.DTOs.Ai;
using LaptopStore.Application.Interfaces;
using LaptopStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LaptopStore.Application.Services;

/// <summary>
/// Trợ lý AI tích hợp nghiệp vụ thực tế, có tool execution và kiểm soát phân quyền người dùng (RBAC).
/// </summary>
public class AiAssistantService : IAiAssistantService
{
    private readonly IApplicationDbContext _context;
    private readonly IProductService _productService;
    private readonly IReportService _reportService;

    public AiAssistantService(
        IApplicationDbContext context,
        IProductService productService,
        IReportService reportService)
    {
        _context = context;
        _productService = productService;
        _reportService = reportService;
    }

    public async Task<AiQueryResponseDto> ProcessUserPromptAsync(string prompt, int currentUserId, UserRole currentRole)
    {
        var response = new AiQueryResponseDto();
        var lower = prompt.Trim().ToLowerInvariant();

        // 1. Phân quyền: Kiểm tra câu hỏi doanh thu / báo cáo tài chính
        if (lower.Contains("doanh thu") || lower.Contains("lợi nhuận") || lower.Contains("báo cáo tài chính") || lower.Contains("doanh số"))
        {
            if (currentRole != UserRole.Admin && currentRole != UserRole.Manager)
            {
                response.Answer = $"[BẢO MẬT & PHÂN QUYỀN]: Bạn đang đăng nhập với vai trò '{currentRole}'. Bạn KHÔNG có quyền truy cập báo cáo doanh thu toàn hệ thống. Chức năng này chỉ dành cho Admin và Quản lý.";
                response.SuggestedAction = "Chuyển tài khoản quản lý hoặc liên hệ Admin nếu cần báo cáo.";
                return response;
            }

            // Thực thi Tool: get_sales_report
            var summary = await _reportService.GetDashboardSummaryAsync();
            var topProducts = await _reportService.GetTopSellingProductsAsync(3);

            response.ExecutedTools.Add(new AiToolCallResultDto
            {
                ToolName = "get_sales_report",
                Arguments = "{}",
                ResultSummary = new { summary.TotalRevenue, summary.MonthRevenue, summary.CompletedOrders }
            });

            var topText = string.Join(", ", topProducts.Select(p => $"{p.ProductName} ({p.TotalQuantitySold} chiếc)"));
            response.Answer = $"📊 [BÁO CÁO DOANH THU HỆ THỐNG]:\n" +
                              $"- Tổng doanh thu tích lũy: {summary.TotalRevenue:N0} đ\n" +
                              $"- Doanh thu tháng này: {summary.MonthRevenue:N0} đ\n" +
                              $"- Tổng số đơn hàng thành công: {summary.CompletedOrders} đơn\n" +
                              $"- Top sản phẩm bán chạy nhất: {topText}.";
            response.SuggestedAction = "Bạn có thể vào tab 'Báo cáo & Thống kê' để xem biểu đồ chi tiết theo tháng.";
            return response;
        }

        // 2. Tra cứu tồn kho / sản phẩm sắp hết hàng
        if (lower.Contains("sắp hết hàng") || lower.Contains("tồn kho thấp") || lower.Contains("hết hàng") || lower.Contains("cần nhập"))
        {
            var lowStock = await _productService.GetLowStockAsync();
            response.ExecutedTools.Add(new AiToolCallResultDto
            {
                ToolName = "get_low_stock_products",
                Arguments = "{}",
                ResultSummary = new { Count = lowStock.Count }
            });

            if (lowStock.Count == 0)
            {
                response.Answer = "✅ Tất cả sản phẩm trong kho hiện tại đều ở mức tồn an toàn (trên mức cảnh báo tối thiểu).";
            }
            else
            {
                var listItems = string.Join("\n", lowStock.Take(5).Select(p => $"• [{p.Sku}] {p.Name} - Còn: {p.StockQuantity} chiếc (Ngưỡng: {p.MinStockLevel})"));
                response.Answer = $"⚠️ Hiện có {lowStock.Count} sản phẩm sắp hết hàng cần bổ sung tồn kho:\n{listItems}";
                response.SuggestedAction = "Nhân viên kho có thể vào màn hình 'Quản lý kho' để lập phiếu nhập hàng.";
            }
            return response;
        }

        // 3. Tư vấn laptop theo ngân sách và nhu cầu (dành cho nhân viên bán hàng hoặc khách)
        var budgetMatch = Regex.Match(lower, @"(\d+)\s*(triệu|tr|m)");
        decimal? budget = null;
        if (budgetMatch.Success && decimal.TryParse(budgetMatch.Groups[1].Value, out var num))
        {
            budget = num * 1_000_000;
        }

        if (lower.Contains("laptop") || lower.Contains("tư vấn") || lower.Contains("gợi ý") || budget.HasValue || lower.Contains("chơi game") || lower.Contains("lập trình") || lower.Contains("văn phòng"))
        {
            var query = _context.Products.Include(p => p.Category).Where(p => p.IsActive && p.StockQuantity > 0);

            if (budget.HasValue)
            {
                query = query.Where(p => p.Price <= budget.Value * 1.15m && p.Price >= budget.Value * 0.7m);
            }

            if (lower.Contains("game") || lower.Contains("đồ họa") || lower.Contains("gaming"))
            {
                query = query.Where(p => p.Gpu != null && p.Gpu != "Intel Iris Xe Graphics" && p.Gpu != "Apple 14-core GPU");
            }
            else if (lower.Contains("văn phòng") || lower.Contains("mỏng nhẹ") || lower.Contains("pin trâu"))
            {
                query = query.Where(p => p.WeightKg == null || p.WeightKg <= 1.8m);
            }

            var recommendations = await query.OrderByDescending(p => p.Price).Take(3).ToListAsync();

            response.ExecutedTools.Add(new AiToolCallResultDto
            {
                ToolName = "search_products_by_requirement",
                Arguments = $"{{ budget: {budget}, requirements: '{prompt}' }}",
                ResultSummary = new { Count = recommendations.Count }
            });

            if (recommendations.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("💡 [GỢI Ý TỪ CƠ SỞ DỮ LIỆU CỬA HÀNG]:");
                foreach (var rec in recommendations)
                {
                    sb.AppendLine($"• {rec.Name} ({rec.Brand} {rec.Model})");
                    sb.AppendLine($"  - Giá: {rec.Price:N0} đ | Tồn kho: {rec.StockQuantity} máy");
                    sb.AppendLine($"  - Cấu hình: CPU {rec.Cpu}, RAM {rec.Ram}, SSD {rec.Storage}, GPU {rec.Gpu}");
                }
                response.Answer = sb.ToString();
                response.SuggestedAction = "Nhân viên có thể tạo đơn hàng ngay cho khách với mã SKU của các máy trên.";
            }
            else
            {
                response.Answer = "Hiện tại cửa hàng không có model nào khớp chính xác với tiêu chí ngân sách/cấu hình này. Bạn có thể mở rộng khoảng ngân sách hoặc kiểm tra các dòng máy văn phòng/gaming khác.";
            }
            return response;
        }

        // Phản hồi chung kèm hướng dẫn gọi lệnh
        response.Answer = "Xin chào! Tôi là Trợ lý AI của TechGear Laptop Store. Tôi có thể hỗ trợ bạn:\n" +
                          "1. Báo cáo doanh thu & đơn hàng (Admin/Manager: 'Doanh thu tháng này thế nào?')\n" +
                          "2. Cảnh báo hàng tồn kho (Tất cả: 'Sản phẩm nào sắp hết hàng?')\n" +
                          "3. Tư vấn & tìm laptop theo nhu cầu/ngân sách (Tất cả: 'Tìm laptop khoảng 25 triệu để lập trình')";
        return response;
    }
}
