using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.Interfaces;
using LaptopStore.Domain.Common;
using LaptopStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LaptopStore.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IApplicationDbContext _context;

    public CategoryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync();

        return categories;
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var c = await _context.Categories
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        return c == null ? null : new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            DisplayOrder = c.DisplayOrder,
            ProductCount = c.Products.Count(p => p.IsActive)
        };
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<CategoryDto>.Failure("Tên danh mục không được để trống.");

        var slug = GenerateSlug(dto.Name);
        var exists = await _context.Categories.AnyAsync(c => c.Slug == slug && c.IsActive);
        if (exists)
            return Result<CategoryDto>.Failure($"Danh mục với tên '{dto.Name}' đã tồn tại.");

        var category = new Category
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Result<CategoryDto>.Success(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            ProductCount = 0
        });
    }

    public async Task<Result<CategoryDto>> UpdateAsync(UpdateCategoryDto dto)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.IsActive);

        if (category == null)
            return Result<CategoryDto>.Failure("Danh mục không tồn tại.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<CategoryDto>.Failure("Tên danh mục không được để trống.");

        var slug = GenerateSlug(dto.Name);
        var conflict = await _context.Categories.AnyAsync(c => c.Slug == slug && c.Id != dto.Id && c.IsActive);
        if (conflict)
            return Result<CategoryDto>.Failure($"Danh mục với tên '{dto.Name}' đã trùng với danh mục khác.");

        category.Name = dto.Name.Trim();
        category.Slug = slug;
        category.Description = dto.Description;
        category.DisplayOrder = dto.DisplayOrder;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Result<CategoryDto>.Success(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            ProductCount = category.Products.Count(p => p.IsActive)
        });
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (category == null)
            return Result.Failure("Danh mục không tồn tại.");

        var hasProducts = category.Products.Any(p => p.IsActive);
        if (hasProducts)
            return Result.Failure("Không thể xóa danh mục đang chứa sản phẩm đang kinh doanh.");

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    private static string GenerateSlug(string text)
    {
        var str = text.ToLowerInvariant();
        str = Regex.Replace(str, @"\s+", "-");
        str = Regex.Replace(str, @"[^\w\-]", "");
        return str.Trim('-');
    }
}
