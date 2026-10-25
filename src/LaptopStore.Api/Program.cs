using LaptopStore.Infrastructure;
using LaptopStore.Infrastructure.Persistence;
using LaptopStore.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký Controllers
builder.Services.AddControllers();

// 2. Đăng ký tầng Infrastructure & Application Services
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Cấu hình JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "LaptopStore_Super_Secret_Key_For_Course_UDQL2_2026_Secure_Token!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LaptopStore";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LaptopStoreApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Cấu hình CORS để Desktop Client và Web Client có thể gọi API không bị chặn
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 5. Cấu hình Swagger với hỗ trợ nhập Bearer Token
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LaptopStore API - Đồ án UDQL2 2026",
        Version = "v1",
        Description = "RESTful Web API cho Hệ thống Quản lý và Bán hàng Laptop & Thiết bị Công nghệ (Nhóm SV: 23120068 - 23120069 - 23210078).\n" +
                      "Tài khoản demo:\n" +
                      "- Admin: admin / Admin@123\n" +
                      "- Quản lý: manager / Manager@123\n" +
                      "- Nhân viên bán hàng: staff / Staff@123\n" +
                      "- Nhân viên kho: warehouse / Warehouse@123"
    });

    // Thêm cấu hình nút Authorize trên Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token JWT theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 6. Tự động khởi tạo Database và nạp Demo Data (Seed Data) khi ứng dụng chạy
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<LaptopStoreDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher>();
        await DbInitializer.SeedAsync(dbContext, hasher);
        app.Logger.LogInformation("Khởi tạo và nạp dữ liệu mẫu Demo thành công.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Đã xảy ra lỗi khi khởi tạo cơ sở dữ liệu và seed data.");
    }
}

// 7. Cấu hình HTTP request pipeline
if (app.Environment.IsDevelopment() || true) // Cho phép bật Swagger để dễ dàng demo và test
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LaptopStore API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
