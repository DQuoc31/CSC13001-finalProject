using LaptopStore.Domain.Enums;

namespace LaptopStore.Application.Security;

/// <summary>
/// Các quyền nghiệp vụ trong hệ thống, dùng chung cho Client (ẩn/hiện chức năng) và chế độ Direct Database (chặn ở tầng service).
/// </summary>
public enum AppPermission
{
    ProductCreate,
    ProductUpdate,
    ProductDelete,
    ProductViewLowStock,
    CategoryCreate,
    CategoryUpdate,
    CategoryDelete,
    CustomerView,
    CustomerCreate,
    CustomerUpdate,
    CustomerDelete,
    OrderView,
    OrderCreate,
    OrderUpdateStatus,
    OrderCancel,
    ReportView,
    AiAsk
}

/// <summary>
/// Ma trận phân quyền RBAC. Phải khớp với các thuộc tính [Authorize(Roles = ...)] trong LaptopStore.Api/Controllers;
/// khi sửa một bên phải sửa cả bên kia.
/// </summary>
public static class RolePermissions
{
    private static readonly UserRole[] AdminOnly = { UserRole.Admin };
    private static readonly UserRole[] AdminManager = { UserRole.Admin, UserRole.Manager };
    private static readonly UserRole[] Sales = { UserRole.Admin, UserRole.Manager, UserRole.SalesStaff };
    private static readonly UserRole[] Warehouse = { UserRole.Admin, UserRole.Manager, UserRole.WarehouseStaff };
    private static readonly UserRole[] AllRoles = { UserRole.Admin, UserRole.Manager, UserRole.SalesStaff, UserRole.WarehouseStaff };

    private static readonly IReadOnlyDictionary<AppPermission, UserRole[]> Matrix = new Dictionary<AppPermission, UserRole[]>
    {
        [AppPermission.ProductCreate] = Warehouse,           // ProductsController POST
        [AppPermission.ProductUpdate] = Warehouse,           // ProductsController PUT
        [AppPermission.ProductDelete] = AdminOnly,           // ProductsController DELETE
        [AppPermission.ProductViewLowStock] = Warehouse,     // ProductsController low-stock, ReportsController low-stock
        [AppPermission.CategoryCreate] = AdminManager,       // CategoriesController POST
        [AppPermission.CategoryUpdate] = AdminManager,       // CategoriesController PUT
        [AppPermission.CategoryDelete] = AdminOnly,          // CategoriesController DELETE
        [AppPermission.CustomerView] = Sales,                // CustomersController GET (+ lịch sử đơn của khách)
        [AppPermission.CustomerCreate] = Sales,              // CustomersController POST
        [AppPermission.CustomerUpdate] = Sales,              // CustomersController PUT
        [AppPermission.CustomerDelete] = AdminOnly,          // CustomersController DELETE
        [AppPermission.OrderView] = AllRoles,                // OrdersController GET
        [AppPermission.OrderCreate] = Sales,                 // OrdersController POST
        [AppPermission.OrderUpdateStatus] = AllRoles,        // OrdersController PUT status
        [AppPermission.OrderCancel] = AdminManager,          // OrdersController cancel
        [AppPermission.ReportView] = AdminManager,           // ReportsController dashboard, top-selling
        [AppPermission.AiAsk] = AllRoles                     // AiController [Authorize]
    };

    /// <summary>
    /// Vai trò có quyền thực hiện thao tác hay không. Chưa đăng nhập (role = null) thì luôn không có quyền.
    /// </summary>
    public static bool Has(UserRole? role, AppPermission permission)
        => role.HasValue && Matrix.TryGetValue(permission, out var roles) && roles.Contains(role.Value);
}
