using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Domain.Common;

namespace LaptopStore.Application.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync(string? searchKeyword = null);
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<CustomerDto?> GetByPhoneAsync(string phone);
    Task<List<OrderDto>> GetCustomerOrdersAsync(int customerId);
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerDto dto);
    Task<Result<CustomerDto>> UpdateAsync(UpdateCustomerDto dto);
    Task<Result> DeleteAsync(int id);
}
