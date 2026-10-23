using LaptopStore.Domain.Common;

namespace LaptopStore.Domain.Entities;

/// <summary>
/// Thực thể khách hàng mua sắm tại cửa hàng.
/// </summary>
public class Customer : BaseEntity
{
    public string CustomerCode { get; set; } = string.Empty; // KH0001, KH0002...
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal TotalSpent { get; set; } = 0;             // Tổng tiền đã mua tích lũy
    public int RewardPoints { get; set; } = 0;              // Điểm thưởng tích lũy
    public string? Note { get; set; }

    // Navigation properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
