using LaptopStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Application.Interfaces;

/// <summary>
/// Interface cho Database Context của hệ thống, giúp tầng Application không bị phụ thuộc chặt vào Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<User> Users { get; }
    DbSet<InventoryTransaction> InventoryTransactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
