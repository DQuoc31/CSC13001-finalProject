using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.Services;
using LaptopStore.Domain.Entities;
using LaptopStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LaptopStore.Application.Tests;

public class OrderTests
{
    [Fact]
    public async Task Order_01_CreateValidOrder_DeductsStockAndCalculatesTotal()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var customer = new Customer { FullName = "Nguyễn Văn A", PhoneNumber = "0901112233", CustomerCode = "KH0001" };
        var category = new Category { Name = "Laptop Gaming", Slug = "laptop-gaming" };
        var product = new Product
        {
            Sku = "ORDER-TEST-01",
            Name = "Lenovo Legion Pro 5",
            Brand = "Lenovo",
            Model = "Pro 5",
            Price = 30000000,
            StockQuantity = 5,
            CategoryId = 1
        };

        context.Customers.Add(customer);
        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var service = new OrderService(context);
        var dto = new CreateOrderDto
        {
            CustomerId = customer.Id,
            DiscountAmount = 1000000,
            PaymentMethod = PaymentMethod.BankTransfer,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = product.Id, Quantity = 2 }
            }
        };

        // Act
        var result = await service.CreateOrderAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(60000000, result.Value.TotalAmount); // 30tr * 2 = 60tr
        Assert.Equal(59000000, result.Value.FinalAmount); // 60tr - 1tr = 59tr

        // Kiểm tra tồn kho sau khi tạo đơn
        var updatedProduct = await context.Products.FindAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.Equal(3, updatedProduct.StockQuantity); // 5 - 2 = 3
    }

    [Fact]
    public async Task Order_02_CreateOrder_ExceedingStock_ReturnsFailure()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var customer = new Customer { FullName = "Trần Thị B", PhoneNumber = "0912223344", CustomerCode = "KH0002" };
        var product = new Product
        {
            Sku = "ORDER-TEST-02",
            Name = "Dell XPS 15",
            Brand = "Dell",
            Model = "XPS",
            Price = 50000000,
            StockQuantity = 2, // Chỉ có 2 máy trong kho
            CategoryId = 1
        };

        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var service = new OrderService(context);
        var dto = new CreateOrderDto
        {
            CustomerId = customer.Id,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = product.Id, Quantity = 5 } // Mua 5 máy > tồn kho 2
            }
        };

        // Act
        var result = await service.CreateOrderAsync(dto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("không đủ đáp ứng", result.Error);

        // Tồn kho không bị trừ
        var p = await context.Products.FindAsync(product.Id);
        Assert.Equal(2, p?.StockQuantity);
    }

    [Fact]
    public async Task Order_03_CreateOrder_ZeroQuantity_ReturnsFailure()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var customer = new Customer { FullName = "Lê Văn C", PhoneNumber = "0923334455", CustomerCode = "KH0003" };
        var product = new Product
        {
            Sku = "ORDER-TEST-03",
            Name = "Chuột MX Master 3S",
            Brand = "Logitech",
            Model = "MX3S",
            Price = 2000000,
            StockQuantity = 10,
            CategoryId = 1
        };
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var service = new OrderService(context);
        var dto = new CreateOrderDto
        {
            CustomerId = customer.Id,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = product.Id, Quantity = 0 } // Số lượng = 0
            }
        };

        // Act
        var result = await service.CreateOrderAsync(dto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("phải lớn hơn 0", result.Error);
    }
}
