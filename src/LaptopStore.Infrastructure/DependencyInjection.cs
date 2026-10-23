using LaptopStore.Application.Interfaces;
using LaptopStore.Application.Services;
using LaptopStore.Infrastructure.Persistence;
using LaptopStore.Infrastructure.Security;
using LaptopStore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LaptopStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Cấu hình Database Context (Mặc định dùng SQLite để chạy ngay lập tức không cần cài đặt SQL Server ngoài)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=laptopstore.db";

        var provider = configuration["DatabaseProvider"] ?? "Sqlite";

        services.AddDbContext<LaptopStoreDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<LaptopStoreDbContext>());

        // 2. Bảo mật & Xác thực
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();

        // 3. Nghiệp vụ Application Services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAiAssistantService, AiAssistantService>();

        return services;
    }
}
