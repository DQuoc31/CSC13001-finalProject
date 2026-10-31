using LaptopStore.Application.DTOs.Auth;
using LaptopStore.Application.Interfaces;
using LaptopStore.Domain.Common;
using LaptopStore.Domain.Entities;
using LaptopStore.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Result<LoginResponseDto>.Failure("Vui lòng nhập tên đăng nhập và mật khẩu.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.Trim().ToLower() && u.IsActive);

        if (user == null)
            return Result<LoginResponseDto>.Failure("Tên đăng nhập hoặc mật khẩu không chính xác.");

        var validPassword = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!validPassword)
            return Result<LoginResponseDto>.Failure("Tên đăng nhập hoặc mật khẩu không chính xác.");

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            UserId = user.Id,
            Token = token,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            ExpiresAt = expiresAt
        });
    }

    public async Task<UserDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Username)
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null)
            return Result.Failure("Tài khoản người dùng không tồn tại.");

        var validOld = _passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);
        if (!validOld)
            return Result.Failure("Mật khẩu hiện tại không chính xác.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return Result.Failure("Mật khẩu mới phải có tối thiểu 6 ký tự.");

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Result.Success();
    }

    private static UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        FullName = u.FullName,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        Role = u.Role,
        IsActive = u.IsActive,
        LastLoginAt = u.LastLoginAt
    };
}
