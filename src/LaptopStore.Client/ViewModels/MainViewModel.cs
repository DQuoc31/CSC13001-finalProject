using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Auth;
using LaptopStore.Client.Converters;
using LaptopStore.Client.Services;
using LaptopStore.Domain.Enums;

namespace LaptopStore.Client.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;

    private object? _currentView;
    private string _currentPage = "products";
    private LoginResponseDto? _currentUser;
    private string _connectionMode;
    private string _loginUsername = "admin";
    private string _loginPassword = "Admin@123";
    private string? _loginError;
    private int _productCount;
    private int _categoryCount;
    private int _customerCount;
    private int _pendingOrderCount;

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    /// <summary>Khoá của trang đang mở (dashboard, products, …) — dùng để tô sáng mục menu.</summary>
    public string CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
                OnPropertyChanged(nameof(PageTitle));
        }
    }

    public string PageTitle => CurrentPage switch
    {
        "dashboard" => "Bảng điều khiển",
        "products" => "Sản phẩm",
        "categories" => "Danh mục",
        "customers" => "Khách hàng",
        "orders" => "Đơn hàng",
        "pos" => "Bán hàng (POS)",
        "ai" => "Trợ lý AI",
        _ => string.Empty
    };

    public LoginResponseDto? CurrentUser
    {
        get => _currentUser;
        set
        {
            if (SetProperty(ref _currentUser, value))
            {
                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(UserDisplayName));
                OnPropertyChanged(nameof(UserRoleName));
                OnPropertyChanged(nameof(UserRoleText));
                OnPropertyChanged(nameof(CanViewReports));
                OnPropertyChanged(nameof(CanManageProducts));
            }
        }
    }

    public bool IsLoggedIn => CurrentUser != null;
    public string UserDisplayName => CurrentUser?.FullName ?? "Chưa đăng nhập";
    public string UserRoleName => CurrentUser?.Role.ToString() ?? "Guest";
    public string UserRoleText => CurrentUser != null ? Display.RoleText(CurrentUser.Role) : "Khách";

    public bool CanViewReports => CurrentUser?.Role == UserRole.Admin || CurrentUser?.Role == UserRole.Manager;
    public bool CanManageProducts => CurrentUser?.Role == UserRole.Admin || CurrentUser?.Role == UserRole.Manager || CurrentUser?.Role == UserRole.WarehouseStaff;

    public string ConnectionMode
    {
        get => _connectionMode;
        set => SetProperty(ref _connectionMode, value);
    }

    /// <summary>Giữa kỳ: ứng dụng kết nối trực tiếp CSDL.</summary>
    public bool IsDirectMode
    {
        get => _apiClient.IsDirectDatabaseMode;
        set { if (value) SetConnectionMode(useApi: false); }
    }

    /// <summary>Cuối kỳ: ứng dụng đi qua Web API.</summary>
    public bool IsApiMode
    {
        get => !_apiClient.IsDirectDatabaseMode;
        set { if (value) SetConnectionMode(useApi: true); }
    }

    public string ConnectionFlow => _apiClient.IsDirectDatabaseMode ? "SQLite · laptopstore.db" : "REST API · localhost:5000";
    public string ConnectionShort => _apiClient.IsDirectDatabaseMode ? "Direct Database" : "Web API";

    public string LoginUsername
    {
        get => _loginUsername;
        set => SetProperty(ref _loginUsername, value);
    }

    public string LoginPassword
    {
        get => _loginPassword;
        set => SetProperty(ref _loginPassword, value);
    }

    public string? LoginError
    {
        get => _loginError;
        set => SetProperty(ref _loginError, value);
    }

    // Số đếm hiển thị trên menu
    public int ProductCount { get => _productCount; set => SetProperty(ref _productCount, value); }
    public int CategoryCount { get => _categoryCount; set => SetProperty(ref _categoryCount, value); }
    public int CustomerCount { get => _customerCount; set => SetProperty(ref _customerCount, value); }
    public int PendingOrderCount { get => _pendingOrderCount; set => SetProperty(ref _pendingOrderCount, value); }

    public string TodayText => DateTime.Now.ToString("dddd, dd/MM/yyyy", Display.Vi);

    // ViewModels con
    public DashboardViewModel DashboardVM { get; }
    public ProductsViewModel ProductsVM { get; }
    public CategoriesViewModel CategoriesVM { get; }
    public CustomersViewModel CustomersVM { get; }
    public OrdersViewModel OrdersVM { get; }
    public PosViewModel PosVM { get; }
    public AiAssistantViewModel AiAssistantVM { get; }

    // Commands
    public IRelayCommand NavigateDashboardCommand { get; }
    public IRelayCommand NavigateProductsCommand { get; }
    public IRelayCommand NavigateCategoriesCommand { get; }
    public IRelayCommand NavigateCustomersCommand { get; }
    public IRelayCommand NavigateOrdersCommand { get; }
    public IRelayCommand NavigatePosCommand { get; }
    public IRelayCommand NavigateAiCommand { get; }
    public IRelayCommand ToggleConnectionModeCommand { get; }
    public IAsyncRelayCommand LoginCommand { get; }
    public IRelayCommand LogoutCommand { get; }
    public IRelayCommand<string> QuickSelectAccountCommand { get; }

    public MainViewModel(
        IApiClient apiClient,
        DashboardViewModel dashboardVM,
        ProductsViewModel productsVM,
        CategoriesViewModel categoriesVM,
        CustomersViewModel customersVM,
        OrdersViewModel ordersVM,
        PosViewModel posVM,
        AiAssistantViewModel aiAssistantVM)
    {
        _apiClient = apiClient;
        DashboardVM = dashboardVM;
        ProductsVM = productsVM;
        CategoriesVM = categoriesVM;
        CustomersVM = customersVM;
        OrdersVM = ordersVM;
        PosVM = posVM;
        AiAssistantVM = aiAssistantVM;

        _connectionMode = _apiClient.ConnectionMode;

        NavigateDashboardCommand = new RelayCommand(() =>
        {
            Show("dashboard", DashboardVM);
            _ = DashboardVM.LoadDataAsync();
        });

        NavigateProductsCommand = new RelayCommand(() =>
        {
            Show("products", ProductsVM);
            _ = ProductsVM.LoadAllAsync();
        });

        NavigateCategoriesCommand = new RelayCommand(() =>
        {
            Show("categories", CategoriesVM);
            _ = CategoriesVM.LoadCategoriesAsync();
        });

        NavigateCustomersCommand = new RelayCommand(() =>
        {
            Show("customers", CustomersVM);
            _ = CustomersVM.LoadCustomersAsync();
        });

        NavigateOrdersCommand = new RelayCommand(() =>
        {
            Show("orders", OrdersVM);
            _ = OrdersVM.LoadOrdersAsync();
        });

        NavigatePosCommand = new RelayCommand(() =>
        {
            Show("pos", PosVM);
            _ = PosVM.LoadAsync();
        });

        NavigateAiCommand = new RelayCommand(() => Show("ai", AiAssistantVM));

        ToggleConnectionModeCommand = new RelayCommand(() => SetConnectionMode(_apiClient.IsDirectDatabaseMode));

        LoginCommand = new AsyncRelayCommand(LoginAsync);
        LogoutCommand = new RelayCommand(Logout);
        QuickSelectAccountCommand = new RelayCommand<string>(QuickSelectAccount);

        // "Lập đơn mới" ở màn Đơn hàng mở trang POS; lập đơn xong thì cập nhật số đếm trên menu
        OrdersVM.CreateOrderRequested += () => NavigatePosCommand.Execute(null);
        PosVM.OrderCreated += () => _ = RefreshCountsAsync();

        // API Server trả 401 (token hết hạn/không hợp lệ) thì xóa phiên trên giao diện
        _apiClient.SessionExpired += OnSessionExpired;

        CurrentView = ProductsVM;
    }

    private void Show(string page, object view)
    {
        CurrentPage = page;
        CurrentView = view;
    }

    private void SetConnectionMode(bool useApi)
    {
        if (useApi == !_apiClient.IsDirectDatabaseMode) return;

        _apiClient.SetConnectionMode(useApi);
        CurrentUser = null; // service đã hủy phiên của chế độ cũ, giao diện cũng phải đăng xuất
        ConnectionMode = _apiClient.ConnectionMode;
        OnPropertyChanged(nameof(IsDirectMode));
        OnPropertyChanged(nameof(IsApiMode));
        OnPropertyChanged(nameof(ConnectionFlow));
        OnPropertyChanged(nameof(ConnectionShort));

        StatusMessage = useApi
            ? "Đã chuyển sang Cuối kỳ: Client ➔ API Server ➔ Database (cần chạy LaptopStore.Api)."
            : "Đã chuyển sang Giữa kỳ: Ứng dụng ➔ Database Server.";
        StatusMessage += " Vui lòng đăng nhập lại.";
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        // HttpClient có thể trả về trên thread khác, cập nhật UI qua Dispatcher
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentUser = null;
            StatusMessage = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
            System.Windows.MessageBox.Show("Phiên đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.",
                "Hết phiên đăng nhập", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        });
    }

    private void QuickSelectAccount(string? roleKey)
    {
        switch (roleKey?.ToLower())
        {
            case "admin":
                LoginUsername = "admin";
                LoginPassword = "Admin@123";
                break;
            case "manager":
                LoginUsername = "manager";
                LoginPassword = "Manager@123";
                break;
            case "staff":
                LoginUsername = "staff";
                LoginPassword = "Staff@123";
                break;
            case "warehouse":
                LoginUsername = "warehouse";
                LoginPassword = "Warehouse@123";
                break;
        }
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(LoginUsername) || string.IsNullOrEmpty(LoginPassword))
        {
            LoginError = "Vui lòng nhập tên đăng nhập và mật khẩu.";
            return;
        }

        IsBusy = true;
        LoginError = null;
        StatusMessage = "Đang xác thực đăng nhập...";
        try
        {
            var result = await _apiClient.LoginAsync(LoginUsername, LoginPassword);
            if (result.IsSuccess && result.Value != null)
            {
                CurrentUser = result.Value;
                StatusMessage = $"Đăng nhập thành công với vai trò: {Display.RoleText(result.Value.Role)}";

                if (CanViewReports) NavigateDashboardCommand.Execute(null);
                else NavigateProductsCommand.Execute(null);

                await RefreshCountsAsync();
            }
            else
            {
                LoginError = result.Error ?? "Tên đăng nhập hoặc mật khẩu không đúng.";
                StatusMessage = LoginError;
            }
        }
        catch (Exception ex)
        {
            LoginError = _apiClient.IsDirectDatabaseMode
                ? $"Không thể kết nối CSDL: {ex.Message}"
                : "Không kết nối được API Server. Hãy chắc chắn 'LaptopStore.Api' đang chạy.";
            StatusMessage = $"Lỗi kết nối ({_apiClient.ConnectionMode}): {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Cập nhật các số đếm trên menu (bỏ qua lỗi để không chặn giao diện).</summary>
    public async Task RefreshCountsAsync()
    {
        try
        {
            ProductCount = (await _apiClient.GetProductsAsync()).Count;
            CategoryCount = (await _apiClient.GetCategoriesAsync()).Count;
            CustomerCount = (await _apiClient.GetCustomersAsync()).Count;
            PendingOrderCount = (await _apiClient.GetOrdersAsync())
                .Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);
        }
        catch
        {
            // Số đếm chỉ để tham khảo
        }
    }

    private void Logout()
    {
        _apiClient.Logout();
        CurrentUser = null;
        StatusMessage = "Đã đăng xuất khỏi hệ thống.";
    }
}
