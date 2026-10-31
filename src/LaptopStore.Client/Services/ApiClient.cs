using LaptopStore.Application.DTOs.Ai;
using LaptopStore.Application.DTOs.Auth;
using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Application.DTOs.Reports;
using LaptopStore.Domain.Common;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LaptopStore.Client.Services;

public interface IApiClient
{
    string? Token { get; set; }
    bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    /// <summary>Người dùng đang đăng nhập (null nếu chưa đăng nhập).</summary>
    LoginResponseDto? CurrentUser { get; }

    /// <summary>Phát ra khi API Server trả 401 (token hết hạn/không hợp lệ) để giao diện đưa người dùng về màn hình đăng nhập.</summary>
    event EventHandler? SessionExpired;
    string ConnectionMode { get; }
    bool IsDirectDatabaseMode { get; }
    void SetConnectionMode(bool useApiServer);

    // Auth
    Task<Result<LoginResponseDto>> LoginAsync(string username, string password);
    void Logout();

    // Products
    Task<List<ProductDto>> GetProductsAsync(string? keyword = null, int? categoryId = null);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<Result> CreateProductAsync(CreateProductDto dto);
    Task<Result> UpdateProductAsync(UpdateProductDto dto);
    Task<Result> DeleteProductAsync(int id);

    // Categories
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<Result> CreateCategoryAsync(CreateCategoryDto dto);
    Task<Result> UpdateCategoryAsync(UpdateCategoryDto dto);
    Task<Result> DeleteCategoryAsync(int id);

    // Customers
    Task<List<CustomerDto>> GetCustomersAsync(string? keyword = null);
    Task<CustomerDto?> GetCustomerByIdAsync(int id);
    Task<Result> CreateCustomerAsync(CreateCustomerDto dto);
    Task<Result> UpdateCustomerAsync(UpdateCustomerDto dto);
    Task<Result> DeleteCustomerAsync(int id);
    Task<List<OrderDto>> GetCustomerOrdersAsync(int customerId);

    // Orders
    Task<List<OrderDto>> GetOrdersAsync();
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<Result<OrderDto>> CreateOrderAsync(CreateOrderDto dto);
    Task<Result> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto);
    Task<Result> CancelOrderAsync(int orderId, string reason);

    // Reports
    Task<DashboardSummaryDto?> GetDashboardSummaryAsync();
    Task<List<TopSellingProductDto>> GetTopSellingProductsAsync();
    Task<List<ProductDto>> GetLowStockProductsAsync();

    // AI Assistant
    Task<AiQueryResponseDto?> AskAiAsync(string prompt);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private string? _token;

    public LoginResponseDto? CurrentUser { get; private set; }
    public event EventHandler? SessionExpired;

    public string ConnectionMode => "API Server Mode (Ứng dụng ➔ API Server ➔ Database Server - Cuối kỳ)";
    public bool IsDirectDatabaseMode => false;

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

    public void SetConnectionMode(bool useApiServer)
    {
        // ApiClient is dedicated to API calls
    }

    public ApiClient(string baseAddress = "http://localhost:5000/")
    {
        _http = new HttpClient(new UnauthorizedHandler(OnUnauthorized)) { BaseAddress = new Uri(baseAddress) };
    }

    public void Logout()
    {
        Token = null;
        CurrentUser = null;
    }

    private void OnUnauthorized()
    {
        // Chỉ coi là hết phiên khi đang đăng nhập; sai mật khẩu lúc đăng nhập cũng trả 401 nhưng không phải hết phiên
        if (CurrentUser == null) return;

        Logout();
        SessionExpired?.Invoke(this, EventArgs.Empty);
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(string username, string password)
    {
        Logout();
        try
        {
            var resp = await _http.PostAsJsonAsync("api/auth/login", new LoginRequestDto { Username = username, Password = password });
            if (!resp.IsSuccessStatusCode)
            {
                // API trả 401 kèm { message } khi sai tài khoản, nên đọc message trước khi dùng thông báo mặc định cho 401
                var message = await ReadMessageAsync(resp);
                return Result<LoginResponseDto>.Failure(message ?? await ReadErrorAsync(resp));
            }

            var result = await resp.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (result == null)
                return Result<LoginResponseDto>.Failure("API Server trả về dữ liệu đăng nhập không hợp lệ.");

            Token = result.Token;
            CurrentUser = result;
            return Result<LoginResponseDto>.Success(result);
        }
        catch (HttpRequestException)
        {
            return Result<LoginResponseDto>.Failure(ConnectionError);
        }
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

    public async Task<Result> CreateProductAsync(CreateProductDto dto)
    {
        return await SendAsync(() => _http.PostAsJsonAsync("api/products", dto));
    }

    public async Task<Result> UpdateProductAsync(UpdateProductDto dto)
    {
        return await SendAsync(() => _http.PutAsJsonAsync($"api/products/{dto.Id}", dto));
    }

    public async Task<Result> DeleteProductAsync(int id)
    {
        return await SendAsync(() => _http.DeleteAsync($"api/products/{id}"));
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var list = await _http.GetFromJsonAsync<List<CategoryDto>>("api/categories");
        return list ?? new();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<CategoryDto>($"api/categories/{id}");
    }

    public async Task<Result> CreateCategoryAsync(CreateCategoryDto dto)
    {
        return await SendAsync(() => _http.PostAsJsonAsync("api/categories", dto));
    }

    public async Task<Result> UpdateCategoryAsync(UpdateCategoryDto dto)
    {
        return await SendAsync(() => _http.PutAsJsonAsync($"api/categories/{dto.Id}", dto));
    }

    public async Task<Result> DeleteCategoryAsync(int id)
    {
        return await SendAsync(() => _http.DeleteAsync($"api/categories/{id}"));
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(string? keyword = null)
    {
        var url = string.IsNullOrEmpty(keyword) ? "api/customers" : $"api/customers?search={Uri.EscapeDataString(keyword)}";
        var list = await _http.GetFromJsonAsync<List<CustomerDto>>(url);
        return list ?? new();
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<CustomerDto>($"api/customers/{id}");
    }

    public async Task<Result> CreateCustomerAsync(CreateCustomerDto dto)
    {
        return await SendAsync(() => _http.PostAsJsonAsync("api/customers", dto));
    }

    public async Task<Result> UpdateCustomerAsync(UpdateCustomerDto dto)
    {
        return await SendAsync(() => _http.PutAsJsonAsync($"api/customers/{dto.Id}", dto));
    }

    public async Task<Result> DeleteCustomerAsync(int id)
    {
        return await SendAsync(() => _http.DeleteAsync($"api/customers/{id}"));
    }

    public async Task<List<OrderDto>> GetCustomerOrdersAsync(int customerId)
    {
        var list = await _http.GetFromJsonAsync<List<OrderDto>>($"api/customers/{customerId}/orders");
        return list ?? new();
    }

    public async Task<List<OrderDto>> GetOrdersAsync()
    {
        var list = await _http.GetFromJsonAsync<List<OrderDto>>("api/orders");
        return list ?? new();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<OrderDto>($"api/orders/{id}");
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderDto dto)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/orders", dto);
            if (!resp.IsSuccessStatusCode)
                return Result<OrderDto>.Failure(await ReadErrorAsync(resp));

            var order = await resp.Content.ReadFromJsonAsync<OrderDto>();
            return order != null
                ? Result<OrderDto>.Success(order)
                : Result<OrderDto>.Failure("API Server trả về dữ liệu đơn hàng không hợp lệ.");
        }
        catch (HttpRequestException)
        {
            return Result<OrderDto>.Failure(ConnectionError);
        }
    }

    public async Task<Result> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto)
    {
        return await SendAsync(() => _http.PutAsJsonAsync($"api/orders/{orderId}/status", dto));
    }

    public async Task<Result> CancelOrderAsync(int orderId, string reason)
    {
        return await SendAsync(() => _http.PostAsJsonAsync($"api/orders/{orderId}/cancel", new { Reason = reason }));
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

    private string ConnectionError => $"Không kết nối được API Server ({_http.BaseAddress}). Hãy chạy LaptopStore.Api hoặc chuyển sang chế độ Direct Database.";

    /// <summary>
    /// Gửi request ghi dữ liệu và chuyển kết quả HTTP thành Result kèm thông báo lỗi từ API.
    /// </summary>
    private async Task<Result> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            var resp = await send();
            return resp.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadErrorAsync(resp));
        }
        catch (HttpRequestException)
        {
            return Result.Failure(ConnectionError);
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage resp)
    {
        switch (resp.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return "Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.";
            case HttpStatusCode.Forbidden:
                return "Tài khoản của bạn không có quyền thực hiện thao tác này.";
        }

        return await ReadMessageAsync(resp) ?? $"Thao tác thất bại (HTTP {(int)resp.StatusCode}).";
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage resp)
    {
        try
        {
            var body = await resp.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return string.IsNullOrWhiteSpace(body?.Message) ? null : body.Message;
        }
        catch (Exception)
        {
            // Body rỗng hoặc không phải JSON dạng { message } (ví dụ lỗi validation mặc định của ASP.NET Core)
            return null;
        }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }

    /// <summary>
    /// Bắt mọi response 401 (kể cả các lệnh GET dùng GetFromJsonAsync) để báo hết phiên đăng nhập.
    /// </summary>
    private sealed class UnauthorizedHandler : DelegatingHandler
    {
        private readonly Action _onUnauthorized;

        public UnauthorizedHandler(Action onUnauthorized) : base(new HttpClientHandler())
        {
            _onUnauthorized = onUnauthorized;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                _onUnauthorized();
            return response;
        }
    }
}
