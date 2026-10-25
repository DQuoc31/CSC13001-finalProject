using LaptopStore.Application.DTOs.Ai;
using LaptopStore.Application.DTOs.Auth;
using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Application.DTOs.Reports;

namespace LaptopStore.Client.Services;

/// <summary>
/// Service trung gian hỗ trợ chuyển đổi linh hoạt giữa 2 kiến trúc:
/// - Chế độ Giữa kỳ: Desktop Client ➔ Database Server (DirectDatabaseService)
/// - Chế độ Cuối kỳ: Desktop Client ➔ API Server ➔ Database Server (ApiClient)
/// </summary>
public class DualModeStoreService : IApiClient
{
    private readonly DirectDatabaseService _directDbService;
    private readonly ApiClient _apiClientService;
    private bool _useApiServer;

    public bool IsDirectDatabaseMode => !_useApiServer;
    public string ConnectionMode => _useApiServer ? _apiClientService.ConnectionMode : _directDbService.ConnectionMode;

    public string? Token
    {
        get => _useApiServer ? _apiClientService.Token : _directDbService.Token;
        set
        {
            if (_useApiServer) _apiClientService.Token = value;
            else _directDbService.Token = value;
        }
    }

    private IApiClient Active => _useApiServer ? _apiClientService : _directDbService;

    public DualModeStoreService(DirectDatabaseService directDbService, ApiClient apiClientService, bool defaultUseApi = false)
    {
        _directDbService = directDbService;
        _apiClientService = apiClientService;
        _useApiServer = defaultUseApi;
    }

    public void SetConnectionMode(bool useApiServer)
    {
        _useApiServer = useApiServer;
    }

    public void Logout() => Active.Logout();

    public Task<LoginResponseDto?> LoginAsync(string username, string password) => Active.LoginAsync(username, password);

    public Task<List<ProductDto>> GetProductsAsync(string? keyword = null, int? categoryId = null) => Active.GetProductsAsync(keyword, categoryId);
    public Task<ProductDto?> GetProductByIdAsync(int id) => Active.GetProductByIdAsync(id);
    public Task<bool> CreateProductAsync(CreateProductDto dto) => Active.CreateProductAsync(dto);
    public Task<bool> UpdateProductAsync(UpdateProductDto dto) => Active.UpdateProductAsync(dto);
    public Task<bool> DeleteProductAsync(int id) => Active.DeleteProductAsync(id);

    public Task<List<CategoryDto>> GetCategoriesAsync() => Active.GetCategoriesAsync();
    public Task<CategoryDto?> GetCategoryByIdAsync(int id) => Active.GetCategoryByIdAsync(id);
    public Task<bool> CreateCategoryAsync(CreateCategoryDto dto) => Active.CreateCategoryAsync(dto);
    public Task<bool> UpdateCategoryAsync(UpdateCategoryDto dto) => Active.UpdateCategoryAsync(dto);
    public Task<bool> DeleteCategoryAsync(int id) => Active.DeleteCategoryAsync(id);

    public Task<List<CustomerDto>> GetCustomersAsync(string? keyword = null) => Active.GetCustomersAsync(keyword);
    public Task<CustomerDto?> GetCustomerByIdAsync(int id) => Active.GetCustomerByIdAsync(id);
    public Task<bool> CreateCustomerAsync(CreateCustomerDto dto) => Active.CreateCustomerAsync(dto);
    public Task<bool> UpdateCustomerAsync(UpdateCustomerDto dto) => Active.UpdateCustomerAsync(dto);
    public Task<bool> DeleteCustomerAsync(int id) => Active.DeleteCustomerAsync(id);
    public Task<List<OrderDto>> GetCustomerOrdersAsync(int customerId) => Active.GetCustomerOrdersAsync(customerId);

    public Task<List<OrderDto>> GetOrdersAsync() => Active.GetOrdersAsync();
    public Task<OrderDto?> GetOrderByIdAsync(int id) => Active.GetOrderByIdAsync(id);
    public Task<OrderDto?> CreateOrderAsync(CreateOrderDto dto) => Active.CreateOrderAsync(dto);
    public Task<bool> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto) => Active.UpdateOrderStatusAsync(orderId, dto);
    public Task<bool> CancelOrderAsync(int orderId, string reason) => Active.CancelOrderAsync(orderId, reason);

    public Task<DashboardSummaryDto?> GetDashboardSummaryAsync() => Active.GetDashboardSummaryAsync();
    public Task<List<TopSellingProductDto>> GetTopSellingProductsAsync() => Active.GetTopSellingProductsAsync();
    public Task<List<ProductDto>> GetLowStockProductsAsync() => Active.GetLowStockProductsAsync();

    public Task<AiQueryResponseDto?> AskAiAsync(string prompt) => Active.AskAiAsync(prompt);
}
