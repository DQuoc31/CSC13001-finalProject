using LaptopStore.Domain.Enums;

namespace LaptopStore.Application.DTOs.Products;

public class ProductDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public string? Cpu { get; set; }
    public string? Ram { get; set; }
    public string? Storage { get; set; }
    public string? Gpu { get; set; }
    public string? ScreenSize { get; set; }
    public string? OperatingSystem { get; set; }
    public decimal? WeightKg { get; set; }
    public int WarrantyMonths { get; set; }
    public ProductStatus Status { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; } = 5;
    public string? Cpu { get; set; }
    public string? Ram { get; set; }
    public string? Storage { get; set; }
    public string? Gpu { get; set; }
    public string? ScreenSize { get; set; }
    public string? OperatingSystem { get; set; }
    public decimal? WeightKg { get; set; }
    public int WarrantyMonths { get; set; } = 24;
    public int CategoryId { get; set; }
}

public class UpdateProductDto : CreateProductDto
{
    public int Id { get; set; }
    public ProductStatus Status { get; set; }
}

public class ProductFilterDto
{
    public string? SearchKeyword { get; set; }
    public int? CategoryId { get; set; }
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStockOnly { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
