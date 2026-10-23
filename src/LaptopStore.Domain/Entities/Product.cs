using LaptopStore.Domain.Common;
using LaptopStore.Domain.Enums;

namespace LaptopStore.Domain.Entities;

/// <summary>
/// Thực thể sản phẩm trong cửa hàng laptop & phần cứng máy tính.
/// </summary>
public class Product : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;       // ASUS, Dell, Apple, Lenovo, MSI, Acer, HP, Kingston, Samsung...
    public string Model { get; set; } = string.Empty;       // ROG Zephyrus G16, XPS 15, MacBook Pro M3...
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    // Giá & Kho
    public decimal Price { get; set; }                      // Giá bán niêm yết (VNĐ)
    public decimal CostPrice { get; set; }                  // Giá vốn nhập hàng (VNĐ)
    public int StockQuantity { get; set; }                  // Số lượng tồn kho hiện tại
    public int MinStockLevel { get; set; } = 5;             // Ngưỡng cảnh báo sắp hết hàng

    // Thông số kỹ thuật đặc thù Laptop / Phần cứng
    public string? Cpu { get; set; }                        // Intel Core i9-14900HX, AMD Ryzen 9 8945HS, Apple M3 Pro...
    public string? Ram { get; set; }                        // 16GB DDR5 5600MHz, 32GB LPDDR5X...
    public string? Storage { get; set; }                    // 1TB PCIe 4.0 NVMe SSD...
    public string? Gpu { get; set; }                        // NVIDIA GeForce RTX 4070 8GB...
    public string? ScreenSize { get; set; }                 // 16.0 inch 2.5K 240Hz OLED...
    public string? OperatingSystem { get; set; }            // Windows 11 Home, macOS Sonoma...
    public decimal? WeightKg { get; set; }                  // 1.85 kg
    public int WarrantyMonths { get; set; } = 24;           // Thời hạn bảo hành (tháng)

    public ProductStatus Status { get; set; } = ProductStatus.Active;

    // Khóa ngoại tới Danh mục
    public int CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    // Navigation properties
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
