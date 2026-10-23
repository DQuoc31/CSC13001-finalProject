namespace LaptopStore.Domain.Enums;

/// <summary>
/// Trạng thái kinh doanh của sản phẩm.
/// </summary>
public enum ProductStatus
{
    Active = 1,        // Đang kinh doanh
    OutOfStock = 2,    // Hết hàng tạm thời
    Discontinued = 3   // Ngừng kinh doanh
}

/// <summary>
/// Loại giao dịch kho để theo dõi lịch sử biến động số lượng tồn.
/// </summary>
public enum StockTransactionType
{
    InitialImport = 1,       // Nhập hàng ban đầu
    Restock = 2,             // Nhập bổ sung tồn kho
    SaleDeduction = 3,       // Xuất bán cho khách hàng
    CustomerReturn = 4,      // Khách trả hàng hoàn lại kho
    InventoryAdjustment = 5  // Điều chỉnh sau kiểm kê kho
}
