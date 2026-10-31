using LaptopStore.Application.Interfaces;
using LaptopStore.Application.Services;
using LaptopStore.Infrastructure.Persistence;
using LaptopStore.Infrastructure.Security;
using LaptopStore.Infrastructure.Services;
using Microsoft.Data.Sqlite;
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
                options.UseSqlite(ResolveSqlitePath(connectionString));
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

    /// <summary>
    /// Đường dẫn SQLite tương đối (vd "laptopstore.db") vốn phụ thuộc thư mục đang chạy, khiến Client (Direct DB) và
    /// API Server mỗi bên tạo một file riêng. Quy về thư mục "data" cạnh LaptopStore.sln để 2 chế độ dùng chung dữ liệu.
    /// </summary>
    private static string ResolveSqlitePath(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || Path.IsPathRooted(builder.DataSource)
            || builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var baseDir = FindSolutionDirectory() is { } solutionDir
            ? Path.Combine(solutionDir, "data")
            : AppContext.BaseDirectory;
        Directory.CreateDirectory(baseDir);

        builder.DataSource = Path.Combine(baseDir, builder.DataSource);
        return builder.ToString();
    }

    private static string? FindSolutionDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "LaptopStore.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
