using LaptopStore.Client.Services;
using LaptopStore.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
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
        // 1. HTTP Api Client
        services.AddSingleton<IApiClient, ApiClient>();

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
