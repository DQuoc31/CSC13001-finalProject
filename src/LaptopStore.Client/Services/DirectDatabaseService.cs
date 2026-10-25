using LaptopStore.Application.DTOs.Ai;
using LaptopStore.Application.DTOs.Auth;
using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Application.DTOs.Reports;
using LaptopStore.Application.Interfaces;
using LaptopStore.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace LaptopStore.Client.Services;

/// <summary>
/// Dịch vụ kết nối trực tiếp CSDL cho giai đoạn Báo cáo tiến độ Giữa kỳ (Ứng dụng ➔ Database Server).
/// Thực thi trực tiếp logic nghiệp vụ thông qua DbContext và Application Services mà không cần Web API.
/// </summary>
public class DirectDatabaseService : IApiClient
{
    private readonly IServiceScopeFactory _scopeFactory;
    private string? _token;
    private UserRole? _currentRole;
    private int? _currentUserId;

    public string ConnectionMode => "Direct Database Mode (Ứng dụng ➔ Database Server - Giai đoạn Giữa kỳ)";
    public bool IsDirectDatabaseMode => true;

    public string? Token
    {
        get => _token;
        set => _token = value;
    }

    public DirectDatabaseService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void SetConnectionMode(bool useApiServer)
    {
    }

    public void Logout()
    {
        _token = null;
        _currentRole = null;
        _currentUserId = null;
    }

    public async Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        using var scope = _scopeFactory.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var result = await authService.LoginAsync(new LoginRequestDto
        {
            Username = username,
            Password = password
        });

        if (result.IsSuccess)
        {
            _token = result.Value.Token;
            _currentRole = result.Value.Role;
            _currentUserId = 1;
            return result.Value;
        }

        return null;
    }

    public async Task<List<ProductDto>> GetProductsAsync(string? keyword = null, int? categoryId = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        return await productService.GetAllAsync(new ProductFilterDto
        {
            SearchKeyword = keyword,
            CategoryId = categoryId,
            PageSize = 200
        });
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        return await productService.GetByIdAsync(id);
    }

    public async Task<bool> CreateProductAsync(CreateProductDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var res = await productService.CreateAsync(dto);
        return res.IsSuccess;
    }

    public async Task<bool> UpdateProductAsync(UpdateProductDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var res = await productService.UpdateAsync(dto);
        return res.IsSuccess;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var res = await productService.DeleteAsync(id);
        return res.IsSuccess;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        return await categoryService.GetAllAsync();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        return await categoryService.GetByIdAsync(id);
    }

    public async Task<bool> CreateCategoryAsync(CreateCategoryDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        var res = await categoryService.CreateAsync(dto);
        return res.IsSuccess;
    }

    public async Task<bool> UpdateCategoryAsync(UpdateCategoryDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        var res = await categoryService.UpdateAsync(dto);
        return res.IsSuccess;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        var res = await categoryService.DeleteAsync(id);
        return res.IsSuccess;
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(string? keyword = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        return await customerService.GetAllAsync(keyword);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        return await customerService.GetByIdAsync(id);
    }

    public async Task<bool> CreateCustomerAsync(CreateCustomerDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var res = await customerService.CreateAsync(dto);
        return res.IsSuccess;
    }

    public async Task<bool> UpdateCustomerAsync(UpdateCustomerDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var res = await customerService.UpdateAsync(dto);
        return res.IsSuccess;
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var res = await customerService.DeleteAsync(id);
        return res.IsSuccess;
    }

    public async Task<List<OrderDto>> GetCustomerOrdersAsync(int customerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        return await customerService.GetCustomerOrdersAsync(customerId);
    }

    public async Task<List<OrderDto>> GetOrdersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        return await orderService.GetAllAsync();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        return await orderService.GetByIdAsync(id);
    }

    public async Task<OrderDto?> CreateOrderAsync(CreateOrderDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var res = await orderService.CreateOrderAsync(dto, _currentUserId);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var res = await orderService.UpdateStatusAsync(orderId, dto);
        return res.IsSuccess;
    }

    public async Task<bool> CancelOrderAsync(int orderId, string reason)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var res = await orderService.CancelOrderAsync(orderId, reason);
        return res.IsSuccess;
    }

    public async Task<DashboardSummaryDto?> GetDashboardSummaryAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        return await reportService.GetDashboardSummaryAsync();
    }

    public async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        return await reportService.GetTopSellingProductsAsync(5);
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        return await productService.GetLowStockAsync();
    }

    public async Task<AiQueryResponseDto?> AskAiAsync(string prompt)
    {
        using var scope = _scopeFactory.CreateScope();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiAssistantService>();
        return await aiService.ProcessUserPromptAsync(prompt, _currentUserId ?? 1, _currentRole ?? UserRole.Admin);
    }
}
