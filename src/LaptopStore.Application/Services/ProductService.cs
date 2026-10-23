using LaptopStore.Application.DTOs.Products;
using LaptopStore.Application.Interfaces;
using LaptopStore.Domain.Common;
using LaptopStore.Domain.Entities;
using LaptopStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Application.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;

    public ProductService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductDto>> GetAllAsync(ProductFilterDto? filter = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .AsNoTracking();

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                var kw = filter.SearchKeyword.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(kw) ||
                    p.Sku.ToLower().Contains(kw) ||
                    p.Brand.ToLower().Contains(kw) ||
                    p.Model.ToLower().Contains(kw) ||
                    (p.Cpu != null && p.Cpu.ToLower().Contains(kw)) ||
                    (p.Gpu != null && p.Gpu.ToLower().Contains(kw)));
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Brand))
            {
                query = query.Where(p => p.Brand == filter.Brand);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            if (filter.InStockOnly == true)
            {
                query = query.Where(p => p.StockQuantity > 0);
            }
        }

        var products = await query
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return products.Select(MapToDto).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var p = await _context.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        return p != null ? MapToDto(p) : null;
    }

    public async Task<ProductDto?> GetBySkuAsync(string sku)
    {
        var p = await _context.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Sku.ToLower() == sku.Trim().ToLower() && x.IsActive);

        return p != null ? MapToDto(p) : null;
    }

    public async Task<List<ProductDto>> GetLowStockAsync()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

        return products.Select(MapToDto).ToList();
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<ProductDto>.Failure("Tên sản phẩm không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.Sku))
            return Result<ProductDto>.Failure("Mã SKU không được để trống.");
        if (dto.Price < 0)
            return Result<ProductDto>.Failure("Giá bán không được nhỏ hơn 0.");
        if (dto.StockQuantity < 0)
            return Result<ProductDto>.Failure("Số lượng tồn kho không được âm.");

        var exists = await _context.Products.AnyAsync(p => p.Sku.ToLower() == dto.Sku.Trim().ToLower());
        if (exists)
            return Result<ProductDto>.Failure($"Mã SKU '{dto.Sku}' đã tồn tại trong hệ thống.");

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.IsActive);
        if (!categoryExists)
            return Result<ProductDto>.Failure("Danh mục sản phẩm không tồn tại.");

        var product = new Product
        {
            Sku = dto.Sku.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            Brand = dto.Brand.Trim(),
            Model = dto.Model.Trim(),
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            Price = dto.Price,
            CostPrice = dto.CostPrice,
            StockQuantity = dto.StockQuantity,
            MinStockLevel = dto.MinStockLevel,
            Cpu = dto.Cpu,
            Ram = dto.Ram,
            Storage = dto.Storage,
            Gpu = dto.Gpu,
            ScreenSize = dto.ScreenSize,
            OperatingSystem = dto.OperatingSystem,
            WeightKg = dto.WeightKg,
            WarrantyMonths = dto.WarrantyMonths,
            CategoryId = dto.CategoryId,
            Status = dto.StockQuantity > 0 ? ProductStatus.Active : ProductStatus.OutOfStock,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<ProductDto>> UpdateAsync(UpdateProductDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.Id && p.IsActive);
        if (product == null)
            return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<ProductDto>.Failure("Tên sản phẩm không được để trống.");
        if (dto.Price < 0)
            return Result<ProductDto>.Failure("Giá bán không được nhỏ hơn 0.");

        var skuConflict = await _context.Products.AnyAsync(p => p.Sku.ToLower() == dto.Sku.Trim().ToLower() && p.Id != dto.Id);
        if (skuConflict)
            return Result<ProductDto>.Failure($"Mã SKU '{dto.Sku}' đã trùng với sản phẩm khác.");

        product.Name = dto.Name.Trim();
        product.Sku = dto.Sku.Trim().ToUpperInvariant();
        product.Brand = dto.Brand.Trim();
        product.Model = dto.Model.Trim();
        product.Description = dto.Description;
        product.ImageUrl = dto.ImageUrl;
        product.Price = dto.Price;
        product.CostPrice = dto.CostPrice;
        product.StockQuantity = dto.StockQuantity;
        product.MinStockLevel = dto.MinStockLevel;
        product.Cpu = dto.Cpu;
        product.Ram = dto.Ram;
        product.Storage = dto.Storage;
        product.Gpu = dto.Gpu;
        product.ScreenSize = dto.ScreenSize;
        product.OperatingSystem = dto.OperatingSystem;
        product.WeightKg = dto.WeightKg;
        product.WarrantyMonths = dto.WarrantyMonths;
        product.CategoryId = dto.CategoryId;
        product.Status = dto.StockQuantity == 0 && dto.Status == ProductStatus.Active
            ? ProductStatus.OutOfStock
            : dto.Status;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        if (product == null)
            return Result.Failure("Sản phẩm không tồn tại.");

        // Soft delete
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> AdjustStockAsync(int productId, int quantityChange, string note, int? userId = null)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
        if (product == null)
            return Result.Failure("Sản phẩm không tồn tại.");

        var newBalance = product.StockQuantity + quantityChange;
        if (newBalance < 0)
            return Result.Failure($"Không thể giảm tồn kho vì số lượng hiện có ({product.StockQuantity}) không đủ.");

        product.StockQuantity = newBalance;
        product.Status = newBalance == 0 ? ProductStatus.OutOfStock : ProductStatus.Active;
        product.UpdatedAt = DateTime.UtcNow;

        var tx = new InventoryTransaction
        {
            ProductId = productId,
            QuantityChange = quantityChange,
            BalanceAfter = newBalance,
            Type = quantityChange >= 0 ? StockTransactionType.Restock : StockTransactionType.InventoryAdjustment,
            Note = note,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.InventoryTransactions.Add(tx);

        await _context.SaveChangesAsync();
        return Result.Success();
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        Brand = p.Brand,
        Model = p.Model,
        Description = p.Description,
        ImageUrl = p.ImageUrl,
        Price = p.Price,
        CostPrice = p.CostPrice,
        StockQuantity = p.StockQuantity,
        MinStockLevel = p.MinStockLevel,
        Cpu = p.Cpu,
        Ram = p.Ram,
        Storage = p.Storage,
        Gpu = p.Gpu,
        ScreenSize = p.ScreenSize,
        OperatingSystem = p.OperatingSystem,
        WeightKg = p.WeightKg,
        WarrantyMonths = p.WarrantyMonths,
        Status = p.Status,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name,
        CreatedAt = p.CreatedAt
    };
}
