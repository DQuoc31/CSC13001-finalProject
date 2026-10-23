using LaptopStore.Application.DTOs.Auth;
using LaptopStore.Domain.Common;

namespace LaptopStore.Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<UserDto?> GetCurrentUserAsync(int userId);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto request);
}
