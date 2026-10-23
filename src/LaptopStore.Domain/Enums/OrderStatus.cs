namespace LaptopStore.Domain.Enums;

/// <summary>
/// Trạng thái đơn hàng.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Đơn hàng mới tạo, chờ xác nhận.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Đã xác nhận, đang đóng gói / chuẩn bị máy.
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Đang vận chuyển tới khách hàng.
    /// </summary>
    Shipping = 3,

    /// <summary>
    /// Giao hàng thành công và hoàn tất thanh toán.
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Đơn hàng bị hủy.
    /// </summary>
    Cancelled = 5
}

/// <summary>
/// Phương thức thanh toán của đơn hàng.
/// </summary>
public enum PaymentMethod
{
    Cash = 1,          // Tiền mặt tại quầy
    CreditCard = 2,    // Thẻ tín dụng/ghi nợ
    BankTransfer = 3,  // Chuyển khoản ngân hàng qua mã QR
    Installment = 4    // Trả góp 0%
}

/// <summary>
/// Trạng thái thanh toán của đơn hàng.
/// </summary>
public enum PaymentStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Refunded = 4
}
