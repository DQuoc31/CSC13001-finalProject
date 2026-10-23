using LaptopStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Application.Tests;

public static class TestDbContextFactory
{
    public static LaptopStoreDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new LaptopStoreDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
