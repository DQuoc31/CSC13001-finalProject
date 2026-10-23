using LaptopStore.Application.DTOs.Products;
using LaptopStore.Application.Services;
using LaptopStore.Domain.Entities;
using Xunit;

namespace LaptopStore.Application.Tests;

public class ProductTests
{
    [Fact]
    public async Task Product_01_CreateValidProduct_ReturnsSuccess()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var category = new Category { Name = "Laptop Gaming", Slug = "laptop-gaming" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new ProductService(context);
        var dto = new CreateProductDto
        {
            Sku = "TEST-LAP-01",
            Name = "ASUS ROG Zephyrus G16",
            Brand = "ASUS",
            Model = "G16",
            Price = 45000000,
            CostPrice = 40000000,
            StockQuantity = 10,
            CategoryId = category.Id
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("TEST-LAP-01", result.Value.Sku);
        Assert.Equal(10, result.Value.StockQuantity);
    }

    [Fact]
    public async Task Product_02_CreateProductWithNegativePrice_ReturnsFailure()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var category = new Category { Name = "Laptop Mỏng nhẹ", Slug = "laptop-mong-nhe" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new ProductService(context);
        var dto = new CreateProductDto
        {
            Sku = "TEST-LAP-02",
            Name = "MacBook Air M3",
            Brand = "Apple",
            Model = "M3",
            Price = -1000, // Giá âm không hợp lệ
            StockQuantity = 5,
            CategoryId = category.Id
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("không được nhỏ hơn 0", result.Error);
    }

    [Fact]
    public async Task Product_03_CreateProductWithDuplicateSku_ReturnsFailure()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var category = new Category { Name = "Linh kiện", Slug = "linh-kien" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new ProductService(context);
        var dto1 = new CreateProductDto
        {
            Sku = "DUPLICATE-SKU",
            Name = "RAM DDR5 32GB",
            Brand = "Kingston",
            Model = "Fury",
            Price = 2500000,
            StockQuantity = 10,
            CategoryId = category.Id
        };
        await service.CreateAsync(dto1);

        // Act
        var dto2 = new CreateProductDto
        {
            Sku = "DUPLICATE-SKU", // Trùng SKU
            Name = "RAM Corsair 32GB",
            Brand = "Corsair",
            Model = "Vengeance",
            Price = 2800000,
            StockQuantity = 10,
            CategoryId = category.Id
        };
        var result = await service.CreateAsync(dto2);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("đã tồn tại", result.Error);
    }
}
