using LaptopStore.Application.Security;
using LaptopStore.Domain.Enums;
using Xunit;

namespace LaptopStore.Application.Tests;

public class RolePermissionsTests
{
    [Theory]
    // Sản phẩm: Admin/Manager/Kho được thêm-sửa, chỉ Admin được xóa
    [InlineData(UserRole.WarehouseStaff, AppPermission.ProductCreate, true)]
    [InlineData(UserRole.SalesStaff, AppPermission.ProductCreate, false)]
    [InlineData(UserRole.Manager, AppPermission.ProductDelete, false)]
    [InlineData(UserRole.Admin, AppPermission.ProductDelete, true)]
    // Danh mục: Admin/Manager quản lý, chỉ Admin được xóa
    [InlineData(UserRole.Manager, AppPermission.CategoryUpdate, true)]
    [InlineData(UserRole.WarehouseStaff, AppPermission.CategoryCreate, false)]
    [InlineData(UserRole.Manager, AppPermission.CategoryDelete, false)]
    // Khách hàng: nhân viên kho không được xem
    [InlineData(UserRole.SalesStaff, AppPermission.CustomerView, true)]
    [InlineData(UserRole.WarehouseStaff, AppPermission.CustomerView, false)]
    [InlineData(UserRole.SalesStaff, AppPermission.CustomerDelete, false)]
    // Đơn hàng: kho không lập đơn, chỉ Admin/Manager được hủy
    [InlineData(UserRole.SalesStaff, AppPermission.OrderCreate, true)]
    [InlineData(UserRole.WarehouseStaff, AppPermission.OrderCreate, false)]
    [InlineData(UserRole.WarehouseStaff, AppPermission.OrderUpdateStatus, true)]
    [InlineData(UserRole.SalesStaff, AppPermission.OrderCancel, false)]
    [InlineData(UserRole.Manager, AppPermission.OrderCancel, true)]
    // Báo cáo: chỉ Admin/Manager
    [InlineData(UserRole.SalesStaff, AppPermission.ReportView, false)]
    [InlineData(UserRole.Manager, AppPermission.ReportView, true)]
    [InlineData(UserRole.WarehouseStaff, AppPermission.ProductViewLowStock, true)]
    [InlineData(UserRole.SalesStaff, AppPermission.ProductViewLowStock, false)]
    public void Rbac_01_PermissionMatrix_MatchesApiAuthorization(UserRole role, AppPermission permission, bool expected)
    {
        Assert.Equal(expected, RolePermissions.Has(role, permission));
    }

    [Fact]
    public void Rbac_02_AdminHasEveryPermission()
    {
        foreach (var permission in Enum.GetValues<AppPermission>())
        {
            Assert.True(RolePermissions.Has(UserRole.Admin, permission), $"Admin thiếu quyền {permission}");
        }
    }

    [Fact]
    public void Rbac_03_NotLoggedIn_HasNoPermission()
    {
        foreach (var permission in Enum.GetValues<AppPermission>())
        {
            Assert.False(RolePermissions.Has(null, permission));
        }
    }
}
