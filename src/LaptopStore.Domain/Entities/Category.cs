using LaptopStore.Domain.Common;

namespace LaptopStore.Domain.Entities;

/// <summary>
/// Danh mục sản phẩm (Laptop Gaming, Laptop Mỏng nhẹ, Workstation, Linh kiện PC, Phụ kiện...)
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;

    // Navigation property
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
