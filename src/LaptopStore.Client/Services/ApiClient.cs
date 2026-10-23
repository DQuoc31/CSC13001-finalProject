using LaptopStore.Application.DTOs.Ai;
using LaptopStore.Application.DTOs.Auth;
using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Application.DTOs.Reports;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LaptopStore.Client.Services;

public interface IApiClient
{
    string? Token { get; set; }
    bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    Task<LoginResponseDto?> LoginAsync(string username, string password);
    Task<List<ProductDto>> GetProductsAsync(string? keyword = null, int? categoryId = null);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<bool> CreateProductAsync(CreateProductDto dto);
    Task<bool> UpdateProductAsync(UpdateProductDto dto);
    Task<bool> DeleteProductAsync(int id);

    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<List<CustomerDto>> GetCustomersAsync(string? keyword = null);
    Task<List<OrderDto>> GetOrdersAsync();
    Task<OrderDto?> CreateOrderAsync(CreateOrderDto dto);

    Task<DashboardSummaryDto?> GetDashboardSummaryAsync();
    Task<List<TopSellingProductDto>> GetTopSellingProductsAsync();
    Task<List<ProductDto>> GetLowStockProductsAsync();

    Task<AiQueryResponseDto?> AskAiAsync(string prompt);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private string? _token;

    public string? Token
    {
        get => _token;
        set
        {
            _token = value;
            if (!string.IsNullOrEmpty(_token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
            else
            {
                _http.DefaultRequestHeaders.Authorization = null;
            }
        }
    }

    public ApiClient(string baseAddress = "http://localhost:5000/")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    public async Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        var resp = await _http.PostAsJsonAsync("api/auth/login", new LoginRequestDto { Username = username, Password = password });
        if (!resp.IsSuccessStatusCode) return null;

        var result = await resp.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (result != null)
        {
            Token = result.Token;
        }
        return result;
    }

    public async Task<List<ProductDto>> GetProductsAsync(string? keyword = null, int? categoryId = null)
    {
        var url = "api/products?";
        if (!string.IsNullOrEmpty(keyword)) url += $"SearchKeyword={Uri.EscapeDataString(keyword)}&";
        if (categoryId.HasValue) url += $"CategoryId={categoryId.Value}&";

        var list = await _http.GetFromJsonAsync<List<ProductDto>>(url);
        return list ?? new();
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<ProductDto>($"api/products/{id}");
    }

    public async Task<bool> CreateProductAsync(CreateProductDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/products", dto);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateProductAsync(UpdateProductDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/products/{dto.Id}", dto);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var resp = await _http.DeleteAsync($"api/products/{id}");
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var list = await _http.GetFromJsonAsync<List<CategoryDto>>("api/categories");
        return list ?? new();
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(string? keyword = null)
    {
        var url = string.IsNullOrEmpty(keyword) ? "api/customers" : $"api/customers?search={Uri.EscapeDataString(keyword)}";
        var list = await _http.GetFromJsonAsync<List<CustomerDto>>(url);
        return list ?? new();
    }

    public async Task<List<OrderDto>> GetOrdersAsync()
    {
        var list = await _http.GetFromJsonAsync<List<OrderDto>>("api/orders");
        return list ?? new();
    }

    public async Task<OrderDto?> CreateOrderAsync(CreateOrderDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/orders", dto);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<OrderDto>();
    }

    public async Task<DashboardSummaryDto?> GetDashboardSummaryAsync()
    {
        return await _http.GetFromJsonAsync<DashboardSummaryDto>("api/reports/dashboard");
    }

    public async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync()
    {
        var list = await _http.GetFromJsonAsync<List<TopSellingProductDto>>("api/reports/top-selling");
        return list ?? new();
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync()
    {
        var list = await _http.GetFromJsonAsync<List<ProductDto>>("api/products/low-stock");
        return list ?? new();
    }

    public async Task<AiQueryResponseDto?> AskAiAsync(string prompt)
    {
        var resp = await _http.PostAsJsonAsync("api/ai/ask", new AiQueryRequestDto { Prompt = prompt });
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<AiQueryResponseDto>();
    }
}
