using LaptopStore.Client.Services;
using LaptopStore.Client.ViewModels;
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

        // 1. HTTP Api Client
        services.AddSingleton<IApiClient>(new ApiClient(apiBaseUrl));

        // 2. ViewModels
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<OrdersViewModel>();
        services.AddTransient<AiAssistantViewModel>();
        services.AddSingleton<MainViewModel>();

        // 3. Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
