namespace LaptopStore.Domain.Common;

/// <summary>
/// Lớp cơ sở cho các thực thể trong hệ thống, cung cấp các trường quản lý thông tin chung.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
