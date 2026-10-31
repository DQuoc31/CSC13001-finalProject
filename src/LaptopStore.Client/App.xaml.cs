using LaptopStore.Application.Interfaces;
using LaptopStore.Client.Services;
using LaptopStore.Client.ViewModels;
using LaptopStore.Infrastructure;
using LaptopStore.Infrastructure.Persistence;
using LaptopStore.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using WpfApplication = System.Windows.Application;

namespace LaptopStore.Client;

public partial class App : WpfApplication
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Đọc cấu hình từ appsettings.json nếu có
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        var configuration = configBuilder.Build();

        var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5000/";
        var defaultUseApi = (configuration["ConnectionMode"] ?? "DirectDatabase")
            .Equals("ApiServer", StringComparison.OrdinalIgnoreCase);

        // JwtTokenGenerator (dùng khi đăng nhập ở chế độ Direct DB) cần IConfiguration để đọc mục Jwt
        services.AddSingleton<IConfiguration>(configuration);

        // 1. Tầng dữ liệu Infrastructure (DbContext, Services, Security) cho chế độ Giữa kỳ (Direct DB)
        services.AddInfrastructureServices(configuration);

        // 2. Đăng ký Dual Mode Data Services (hỗ trợ cả Giữa kỳ và Cuối kỳ)
        services.AddSingleton(new ApiClient(apiBaseUrl));
        services.AddSingleton<DirectDatabaseService>();
        services.AddSingleton<IApiClient>(sp => new DualModeStoreService(
            sp.GetRequiredService<DirectDatabaseService>(),
            sp.GetRequiredService<ApiClient>(),
            defaultUseApi));

        // 3. ViewModels
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<OrdersViewModel>();
        services.AddTransient<PosViewModel>();
        services.AddTransient<AiAssistantViewModel>();
        services.AddSingleton<MainViewModel>();

        // 4. Views
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Định dạng số/ngày theo tiếng Việt cho toàn bộ binding (52.990.000 đ, 31/10/2026)
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = vi;
        CultureInfo.DefaultThreadCurrentUICulture = vi;
        Thread.CurrentThread.CurrentCulture = vi;
        Thread.CurrentThread.CurrentUICulture = vi;
        var viLanguage = XmlLanguage.GetLanguage(vi.IetfLanguageTag);
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(viLanguage));
        // <Run> trong TextBlock là FrameworkContentElement nên cần đặt riêng
        FrameworkContentElement.LanguageProperty.OverrideMetadata(typeof(System.Windows.Documents.TextElement), new FrameworkPropertyMetadata(viLanguage));

        base.OnStartup(e);

        // Tự động khởi tạo Database và nạp dữ liệu mẫu Demo (Giữa kỳ: Ứng dụng -> Database)
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LaptopStoreDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await DbInitializer.SeedAsync(db, hasher);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Seed Error: {ex.Message}");
        }

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
