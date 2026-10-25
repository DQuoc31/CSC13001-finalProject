using LaptopStore.Application.Interfaces;
using LaptopStore.Client.Services;
using LaptopStore.Client.ViewModels;
using LaptopStore.Infrastructure;
using LaptopStore.Infrastructure.Persistence;
using LaptopStore.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
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
        services.AddTransient<OrdersViewModel>();
        services.AddTransient<AiAssistantViewModel>();
        services.AddSingleton<MainViewModel>();

        // 4. Views
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
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
