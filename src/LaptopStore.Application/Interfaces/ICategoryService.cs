using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Domain.Common;

namespace LaptopStore.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto);
    Task<Result<CategoryDto>> UpdateAsync(UpdateCategoryDto dto);
    Task<Result> DeleteAsync(int id);
}
