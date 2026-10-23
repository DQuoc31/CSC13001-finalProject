using LaptopStore.Application.DTOs.Auth;
using LaptopStore.Domain.Entities;
using LaptopStore.Domain.Enums;
using LaptopStore.Infrastructure.Security;
using LaptopStore.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LaptopStore.Application.Tests;

public class AuthTests
{
    private readonly IPasswordHasher _hasher = new PasswordHasher();
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", "Test_Secret_Key_For_Unit_Testing_1234567890123456" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" },
                { "Jwt:ExpiryMinutes", "60" }
            })
            .Build();

        _tokenGenerator = new JwtTokenGenerator(config);
    }

    [Fact]
    public async Task Login_01_CorrectCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var user = new User
        {
            Username = "admin",
            PasswordHash = _hasher.HashPassword("Admin@123"),
            FullName = "Admin User",
            Role = UserRole.Admin
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthService(context, _hasher, _tokenGenerator);

        // Act
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "admin",
            Password = "Admin@123"
        });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Token);
        Assert.Equal("admin", result.Value.Username);
        Assert.Equal(UserRole.Admin, result.Value.Role);
    }

    [Fact]
    public async Task Login_02_IncorrectPassword_ReturnsFailure()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var user = new User
        {
            Username = "staff",
            PasswordHash = _hasher.HashPassword("Staff@123"),
            FullName = "Staff User",
            Role = UserRole.SalesStaff
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthService(context, _hasher, _tokenGenerator);

        // Act
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "staff",
            Password = "WrongPassword" // Mật khẩu sai
        });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("không chính xác", result.Error);
    }

    [Fact]
    public async Task Login_03_NonExistentUser_ReturnsFailure()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var service = new AuthService(context, _hasher, _tokenGenerator);

        // Act
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "ghost_user",
            Password = "Password123"
        });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("không chính xác", result.Error);
    }
}
