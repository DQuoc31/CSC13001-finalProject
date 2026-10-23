using LaptopStore.Domain.Common;
using LaptopStore.Domain.Enums;

namespace LaptopStore.Domain.Entities;

/// <summary>
/// Nhật ký biến động tồn kho (nhập hàng, bán hàng, hoàn trả, kiểm kê).
/// </summary>
public class InventoryTransaction : BaseEntity
{
    public int ProductId { get; set; }
    public virtual Product? Product { get; set; }

    public int QuantityChange { get; set; }          // Dương là cộng vào kho, âm là trừ khỏi kho
    public int BalanceAfter { get; set; }             // Số lượng tồn sau giao dịch
    public StockTransactionType Type { get; set; }   // Loại giao dịch
    public string? ReferenceDocument { get; set; }    // Mã hóa đơn hoặc phiếu nhập
    public string? Note { get; set; }
    public int? CreatedByUserId { get; set; }
}
