using LaptopStore.Application.DTOs.Products;
using LaptopStore.Domain.Common;

namespace LaptopStore.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(ProductFilterDto? filter = null);
    Task<ProductDto?> GetByIdAsync(int id);
    Task<ProductDto?> GetBySkuAsync(string sku);
    Task<List<ProductDto>> GetLowStockAsync();
    Task<Result<ProductDto>> CreateAsync(CreateProductDto dto);
    Task<Result<ProductDto>> UpdateAsync(UpdateProductDto dto);
    Task<Result> DeleteAsync(int id);
    Task<Result> AdjustStockAsync(int productId, int quantityChange, string note, int? userId = null);
}
