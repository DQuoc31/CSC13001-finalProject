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

    private static async Task<(LaptopStore.Infrastructure.Persistence.LaptopStoreDbContext Context, Customer Customer, Product Product, OrderService Service, int OrderId)> CreatePendingOrderAsync()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var customer = new Customer { FullName = "Trần Thị B", PhoneNumber = "0907778899", CustomerCode = "KH0002" };
        var category = new Category { Name = "Laptop Văn Phòng", Slug = "laptop-van-phong" };
        var product = new Product
        {
            Sku = "ORDER-TEST-CANCEL",
            Name = "Dell XPS 13",
            Brand = "Dell",
            Model = "XPS 13",
            Price = 25000000,
            StockQuantity = 4,
            CategoryId = 1
        };

        context.Customers.Add(customer);
        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var service = new OrderService(context);
        var result = await service.CreateOrderAsync(new CreateOrderDto
        {
            CustomerId = customer.Id,
            PaymentMethod = PaymentMethod.Cash,
            Items = new List<CreateOrderItemDto> { new() { ProductId = product.Id, Quantity = 2 } }
        });
        Assert.True(result.IsSuccess);

        return (context, customer, product, service, result.Value.Id);
    }

    [Fact]
    public async Task Order_04_CancelOrder_RestoresStockAndRevertsCustomerSpendingAndPoints()
    {
        // Arrange
        var (context, customer, product, service, orderId) = await CreatePendingOrderAsync();
        using var _ = context;
        Assert.Equal(50000000, customer.TotalSpent);  // 25tr * 2
        Assert.Equal(500, customer.RewardPoints);    // 100k = 1 điểm

        // Act
        var cancel = await service.CancelOrderAsync(orderId, "Khách đổi ý");

        // Assert
        Assert.True(cancel.IsSuccess);
        Assert.Equal(4, product.StockQuantity);
        Assert.Equal(0, customer.TotalSpent);
        Assert.Equal(0, customer.RewardPoints);
    }

    [Fact]
    public async Task Order_05_CompletedOrder_CannotBeCancelled()
    {
        // Arrange
        var (context, _, product, service, orderId) = await CreatePendingOrderAsync();
        using var _ctx = context;
        await service.UpdateStatusAsync(orderId, new UpdateOrderStatusDto { Status = OrderStatus.Completed });

        // Act
        var cancel = await service.CancelOrderAsync(orderId, "Thử hủy");

        // Assert
        Assert.True(cancel.IsFailure);
        Assert.Contains("đã hoàn tất", cancel.Error);
        Assert.Equal(2, product.StockQuantity);
    }

    [Fact]
    public async Task Order_06_CancelledOrder_CannotChangeStatusOrBeCancelledViaStatusUpdate()
    {
        // Arrange
        var (context, _, _, service, orderId) = await CreatePendingOrderAsync();
        using var _ctx = context;

        // Act: hủy qua UpdateStatus phải bị chặn (không hoàn kho)
        var cancelViaStatus = await service.UpdateStatusAsync(orderId, new UpdateOrderStatusDto { Status = OrderStatus.Cancelled });
        await service.CancelOrderAsync(orderId, "Khách đổi ý");
        var reopen = await service.UpdateStatusAsync(orderId, new UpdateOrderStatusDto { Status = OrderStatus.Processing });

        // Assert
        Assert.True(cancelViaStatus.IsFailure);
        Assert.True(reopen.IsFailure);
        Assert.Contains("đã bị hủy", reopen.Error);
    }
}
