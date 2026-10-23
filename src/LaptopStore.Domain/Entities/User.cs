using LaptopStore.Domain.Common;
using LaptopStore.Domain.Enums;

namespace LaptopStore.Domain.Entities;

/// <summary>
/// Thực thể tài khoản nhân viên / người dùng hệ thống phục vụ Authentication & RBAC.
/// </summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.SalesStaff;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<Order> CreatedOrders { get; set; } = new List<Order>();
}
