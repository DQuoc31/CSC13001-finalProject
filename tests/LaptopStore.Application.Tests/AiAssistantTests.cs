using LaptopStore.Application.Services;
using LaptopStore.Domain.Entities;
using LaptopStore.Domain.Enums;
using Xunit;

namespace LaptopStore.Application.Tests;

public class AiAssistantTests
{
    [Fact]
    public async Task Ai_01_SalesStaff_AskingRevenue_DeniedByRbac()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var prodService = new ProductService(context);
        var reportService = new ReportService(context);
        var aiService = new AiAssistantService(context, prodService, reportService);

        // Act (Nhân viên bán hàng hỏi doanh thu toàn hệ thống)
        var response = await aiService.ProcessUserPromptAsync("Cho tôi biết doanh thu toàn hệ thống tháng này?", 1, UserRole.SalesStaff);

        // Assert
        Assert.NotNull(response.Answer);
        Assert.Contains("KHÔNG có quyền truy cập", response.Answer);
        Assert.Empty(response.ExecutedTools); // Không được phép gọi tool báo cáo
    }

    [Fact]
    public async Task Ai_02_Manager_AskingRevenue_AllowedAndExecutesTool()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var customer = new Customer { FullName = "Khách A", PhoneNumber = "0901234567", CustomerCode = "KH0001" };
        var order = new Order
        {
            OrderNumber = "HD-TEST-REVENUE",
            CustomerId = 1,
            TotalAmount = 50000000,
            FinalAmount = 50000000,
            Status = OrderStatus.Completed
        };
        context.Customers.Add(customer);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var prodService = new ProductService(context);
        var reportService = new ReportService(context);
        var aiService = new AiAssistantService(context, prodService, reportService);

        // Act (Quản lý hỏi doanh thu)
        var response = await aiService.ProcessUserPromptAsync("Báo cáo doanh thu tháng này của cửa hàng", 2, UserRole.Manager);

        // Assert
        Assert.NotNull(response.Answer);
        Assert.Contains("DOANH THU", response.Answer);
        Assert.Contains(response.ExecutedTools, t => t.ToolName == "get_sales_report");
    }

    [Fact]
    public async Task Ai_03_AskForLaptopRecommendation_ReturnsRealProducts()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var category = new Category { Name = "Laptop Gaming", Slug = "laptop-gaming" };
        var product = new Product
        {
            Sku = "AI-REC-01",
            Name = "MSI Katana 15 Gaming",
            Brand = "MSI",
            Model = "Katana 15",
            Price = 25000000,
            StockQuantity = 5,
            Cpu = "Intel Core i7-13620H",
            Gpu = "RTX 4060",
            CategoryId = 1
        };
        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var prodService = new ProductService(context);
        var reportService = new ReportService(context);
        var aiService = new AiAssistantService(context, prodService, reportService);

        // Act
        var response = await aiService.ProcessUserPromptAsync("Tôi có ngân sách 25 triệu cần mua laptop để chơi game", 3, UserRole.SalesStaff);

        // Assert
        Assert.NotNull(response.Answer);
        Assert.Contains("MSI Katana 15 Gaming", response.Answer);
        Assert.Contains(response.ExecutedTools, t => t.ToolName == "search_products_by_requirement");
    }
}
