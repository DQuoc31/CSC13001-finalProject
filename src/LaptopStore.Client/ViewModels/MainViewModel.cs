using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Auth;
using LaptopStore.Client.Services;
using System.Windows;

namespace LaptopStore.Client.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;

    private object? _currentView;
    private LoginResponseDto? _currentUser;
    private string _connectionMode;
    private string _loginUsername = "admin";
    private string _loginPassword = "Admin@123";

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

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
                OnPropertyChanged(nameof(CanViewReports));
                OnPropertyChanged(nameof(CanManageProducts));
            }
        }
    }

    public bool IsLoggedIn => CurrentUser != null;
    public string UserDisplayName => CurrentUser?.FullName ?? "Chưa đăng nhập";
    public string UserRoleName => CurrentUser?.Role.ToString() ?? "Guest";

    public bool CanViewReports => CurrentUser?.Role == Domain.Enums.UserRole.Admin || CurrentUser?.Role == Domain.Enums.UserRole.Manager;
    public bool CanManageProducts => CurrentUser?.Role == Domain.Enums.UserRole.Admin || CurrentUser?.Role == Domain.Enums.UserRole.Manager || CurrentUser?.Role == Domain.Enums.UserRole.WarehouseStaff;

    public string ConnectionMode
    {
        get => _connectionMode;
        set => SetProperty(ref _connectionMode, value);
    }

    public string ConnectionModeBadge => _apiClient.IsDirectDatabaseMode
        ? "⚡ Chế độ Giữa kỳ: Direct Database (Ứng dụng ➔ Database Server)"
        : "🌐 Chế độ Cuối kỳ: Web API (Client ➔ API Server ➔ Database Server)";

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

    // ViewModels con
    public DashboardViewModel DashboardVM { get; }
    public ProductsViewModel ProductsVM { get; }
    public CategoriesViewModel CategoriesVM { get; }
    public CustomersViewModel CustomersVM { get; }
    public OrdersViewModel OrdersVM { get; }
    public AiAssistantViewModel AiAssistantVM { get; }

    // Commands
    public IRelayCommand NavigateDashboardCommand { get; }
    public IRelayCommand NavigateProductsCommand { get; }
    public IRelayCommand NavigateCategoriesCommand { get; }
    public IRelayCommand NavigateCustomersCommand { get; }
    public IRelayCommand NavigateOrdersCommand { get; }
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
        AiAssistantViewModel aiAssistantVM)
    {
        _apiClient = apiClient;
        DashboardVM = dashboardVM;
        ProductsVM = productsVM;
        CategoriesVM = categoriesVM;
        CustomersVM = customersVM;
        OrdersVM = ordersVM;
        AiAssistantVM = aiAssistantVM;

        _connectionMode = _apiClient.ConnectionMode;

        NavigateDashboardCommand = new RelayCommand(() =>
        {
            CurrentView = DashboardVM;
            _ = DashboardVM.LoadDataAsync();
        });

        NavigateProductsCommand = new RelayCommand(() =>
        {
            CurrentView = ProductsVM;
            _ = ProductsVM.LoadAllAsync();
        });

        NavigateCategoriesCommand = new RelayCommand(() =>
        {
            CurrentView = CategoriesVM;
            _ = CategoriesVM.LoadCategoriesAsync();
        });

        NavigateCustomersCommand = new RelayCommand(() =>
        {
            CurrentView = CustomersVM;
            _ = CustomersVM.LoadCustomersAsync();
        });

        NavigateOrdersCommand = new RelayCommand(() =>
        {
            CurrentView = OrdersVM;
            _ = OrdersVM.LoadOrdersAsync();
        });

        NavigateAiCommand = new RelayCommand(() =>
        {
            CurrentView = AiAssistantVM;
        });

        ToggleConnectionModeCommand = new RelayCommand(ToggleConnectionMode);

        LoginCommand = new AsyncRelayCommand(LoginAsync);
        LogoutCommand = new RelayCommand(Logout);
        QuickSelectAccountCommand = new RelayCommand<string>(QuickSelectAccount);

        // Mặc định ban đầu chọn màn hình Sản phẩm
        CurrentView = ProductsVM;
    }

    private void ToggleConnectionMode()
    {
        bool newApiMode = _apiClient.IsDirectDatabaseMode; // toggle
        _apiClient.SetConnectionMode(newApiMode);
        ConnectionMode = _apiClient.ConnectionMode;
        OnPropertyChanged(nameof(ConnectionModeBadge));

        string modeName = newApiMode ? "Cuối kỳ (Client ➔ API Server ➔ Database)" : "Giữa kỳ (Ứng dụng ➔ Database Server)";
        StatusMessage = $"Đã chuyển sang: {modeName}";
        MessageBox.Show(
            $"Đã chuyển kiến trúc sang:\n{_apiClient.ConnectionMode}\n\n(Lưu ý: Nếu dùng API Server, hãy chắc chắn 'LaptopStore.Api' đang chạy).",
            "Chuyển Đổi Kiến Trúc Hoạt Động",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
        IsBusy = true;
        StatusMessage = "Đang xác thực đăng nhập...";
        try
        {
            var res = await _apiClient.LoginAsync(LoginUsername, LoginPassword);
            if (res != null)
            {
                CurrentUser = res;
                StatusMessage = $"Đăng nhập thành công với vai trò: {res.Role}";
                MessageBox.Show($"Chào mừng {res.FullName} ({res.Role}) đăng nhập thành công!", "Đăng nhập", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
                MessageBox.Show("Đăng nhập thất bại. Vui lòng kiểm tra lại tài khoản demo hoặc đảm bảo API Server đang chạy.", "Lỗi đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi kết nối ({_apiClient.ConnectionMode}): {ex.Message}";
            MessageBox.Show($"Không thể kết nối:\n{ex.Message}\n\nNếu đang ở chế độ API Server, hãy chắc chắn 'LaptopStore.Api' đã được khởi chạy.", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Logout()
    {
        _apiClient.Logout();
        CurrentUser = null;
        StatusMessage = "Đã đăng xuất khỏi hệ thống.";
    }
}
