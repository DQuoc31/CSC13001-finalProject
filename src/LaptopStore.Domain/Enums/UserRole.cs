namespace LaptopStore.Domain.Enums;

/// <summary>
/// Vai trò của người dùng trong hệ thống (RBAC).
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Quản trị viên: Toàn quyền trên toàn bộ hệ thống, quản lý tài khoản, cấu hình.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Quản lý: Xem báo cáo thống kê, theo dõi doanh thu, giám sát hoạt động kinh doanh.
    /// </summary>
    Manager = 2,

    /// <summary>
    /// Nhân viên bán hàng: Lập đơn hàng, xử lý bán hàng, tra cứu thông tin sản phẩm và khách hàng.
    /// </summary>
    SalesStaff = 3,

    /// <summary>
    /// Nhân viên kho: Quản lý tồn kho, nhập hàng, cập nhật tình trạng sản phẩm và kiểm kê.
    /// </summary>
    WarehouseStaff = 4
}
