using LaptopStore.Application.DTOs.Ai;
using LaptopStore.Application.DTOs.Auth;
using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Application.DTOs.Reports;
using LaptopStore.Domain.Common;
using LaptopStore.Application.Interfaces;
using LaptopStore.Application.Security;
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
    private LoginResponseDto? _currentUser;

    public LoginResponseDto? CurrentUser => _currentUser;

    // Chế độ Direct DB không có token hết hạn giữa chừng nên không bao giờ phát sự kiện này
    public event EventHandler? SessionExpired { add { } remove { } }

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
        _currentUser = null;
    }

    /// <summary>
    /// Ở chế độ Direct DB, client đóng vai trò "server" nên phải tự kiểm tra quyền như các [Authorize] của Web API.
    /// </summary>
    private string? PermissionError(AppPermission permission)
    {
        if (_currentUser == null)
            return "Vui lòng đăng nhập để sử dụng chức năng này.";

        return RolePermissions.Has(_currentUser.Role, permission)
            ? null
            : $"Vai trò '{_currentUser.Role}' không có quyền thực hiện thao tác này.";
    }

    private void Require(AppPermission permission)
    {
        var error = PermissionError(permission);
        if (error != null)
            throw new UnauthorizedAccessException(error);
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(string username, string password)
    {
        Logout();

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
            _currentUser = result.Value;
        }

        return result;
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

    public async Task<Result> CreateProductAsync(CreateProductDto dto)
    {
        if (PermissionError(AppPermission.ProductCreate) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var res = await productService.CreateAsync(dto);
        return res;
    }

    public async Task<Result> UpdateProductAsync(UpdateProductDto dto)
    {
        if (PermissionError(AppPermission.ProductUpdate) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var res = await productService.UpdateAsync(dto);
        return res;
    }

    public async Task<Result> DeleteProductAsync(int id)
    {
        if (PermissionError(AppPermission.ProductDelete) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var res = await productService.DeleteAsync(id);
        return res;
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

    public async Task<Result> CreateCategoryAsync(CreateCategoryDto dto)
    {
        if (PermissionError(AppPermission.CategoryCreate) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        var res = await categoryService.CreateAsync(dto);
        return res;
    }

    public async Task<Result> UpdateCategoryAsync(UpdateCategoryDto dto)
    {
        if (PermissionError(AppPermission.CategoryUpdate) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        var res = await categoryService.UpdateAsync(dto);
        return res;
    }

    public async Task<Result> DeleteCategoryAsync(int id)
    {
        if (PermissionError(AppPermission.CategoryDelete) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        var res = await categoryService.DeleteAsync(id);
        return res;
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(string? keyword = null)
    {
        Require(AppPermission.CustomerView);

        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        return await customerService.GetAllAsync(keyword);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
    {
        Require(AppPermission.CustomerView);

        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        return await customerService.GetByIdAsync(id);
    }

    public async Task<Result> CreateCustomerAsync(CreateCustomerDto dto)
    {
        if (PermissionError(AppPermission.CustomerCreate) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var res = await customerService.CreateAsync(dto);
        return res;
    }

    public async Task<Result> UpdateCustomerAsync(UpdateCustomerDto dto)
    {
        if (PermissionError(AppPermission.CustomerUpdate) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var res = await customerService.UpdateAsync(dto);
        return res;
    }

    public async Task<Result> DeleteCustomerAsync(int id)
    {
        if (PermissionError(AppPermission.CustomerDelete) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var res = await customerService.DeleteAsync(id);
        return res;
    }

    public async Task<List<OrderDto>> GetCustomerOrdersAsync(int customerId)
    {
        Require(AppPermission.CustomerView);

        using var scope = _scopeFactory.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        return await customerService.GetCustomerOrdersAsync(customerId);
    }

    public async Task<List<OrderDto>> GetOrdersAsync()
    {
        Require(AppPermission.OrderView);

        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        return await orderService.GetAllAsync();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        Require(AppPermission.OrderView);

        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        return await orderService.GetByIdAsync(id);
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderDto dto)
    {
        if (PermissionError(AppPermission.OrderCreate) is { } denied) return Result<OrderDto>.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var res = await orderService.CreateOrderAsync(dto, _currentUser!.UserId);
        return res;
    }

    public async Task<Result> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto)
    {
        if (PermissionError(AppPermission.OrderUpdateStatus) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var res = await orderService.UpdateStatusAsync(orderId, dto);
        return res;
    }

    public async Task<Result> CancelOrderAsync(int orderId, string reason)
    {
        if (PermissionError(AppPermission.OrderCancel) is { } denied) return Result.Failure(denied);

        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var res = await orderService.CancelOrderAsync(orderId, reason);
        return res;
    }

    public async Task<DashboardSummaryDto?> GetDashboardSummaryAsync()
    {
        Require(AppPermission.ReportView);

        using var scope = _scopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        return await reportService.GetDashboardSummaryAsync();
    }

    public async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync()
    {
        Require(AppPermission.ReportView);

        using var scope = _scopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        return await reportService.GetTopSellingProductsAsync(5);
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync()
    {
        Require(AppPermission.ProductViewLowStock);

        using var scope = _scopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        return await productService.GetLowStockAsync();
    }

    public async Task<AiQueryResponseDto?> AskAiAsync(string prompt)
    {
        Require(AppPermission.AiAsk);

        using var scope = _scopeFactory.CreateScope();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiAssistantService>();
        return await aiService.ProcessUserPromptAsync(prompt, _currentUser!.UserId, _currentUser.Role);
    }
}
