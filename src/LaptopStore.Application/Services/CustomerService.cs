using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.Interfaces;
using LaptopStore.Domain.Common;
using LaptopStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _context;

    public CustomerService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustomerDto>> GetAllAsync(string? searchKeyword = null)
    {
        var query = _context.Customers
            .Include(c => c.Orders)
            .Where(c => c.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(kw) ||
                c.PhoneNumber.Contains(kw) ||
                c.CustomerCode.ToLower().Contains(kw) ||
                (c.Email != null && c.Email.ToLower().Contains(kw)));
        }

        var list = await query
            .OrderByDescending(c => c.Id)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var c = await _context.Customers
            .Include(x => x.Orders)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        return c != null ? MapToDto(c) : null;
    }

    public async Task<CustomerDto?> GetByPhoneAsync(string phone)
    {
        var cleanPhone = phone.Trim();
        var c = await _context.Customers
            .Include(x => x.Orders)
            .FirstOrDefaultAsync(x => x.PhoneNumber == cleanPhone && x.IsActive);

        return c != null ? MapToDto(c) : null;
    }

    public async Task<List<OrderDto>> GetCustomerOrdersAsync(int customerId)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.CustomerId == customerId && o.IsActive)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            TotalAmount = o.TotalAmount,
            DiscountAmount = o.DiscountAmount,
            FinalAmount = o.FinalAmount,
            Status = o.Status,
            PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus,
            CreatedAt = o.CreatedAt
        }).ToList();
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return Result<CustomerDto>.Failure("Tên khách hàng không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            return Result<CustomerDto>.Failure("Số điện thoại không được để trống.");

        var exists = await _context.Customers.AnyAsync(c => c.PhoneNumber == dto.PhoneNumber.Trim() && c.IsActive);
        if (exists)
            return Result<CustomerDto>.Failure($"Số điện thoại '{dto.PhoneNumber}' đã thuộc về khách hàng khác.");

        var nextId = (await _context.Customers.MaxAsync(c => (int?)c.Id) ?? 0) + 1;
        var customer = new Customer
        {
            CustomerCode = $"KH{nextId:D4}",
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> UpdateAsync(UpdateCustomerDto dto)
    {
        var customer = await _context.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.IsActive);

        if (customer == null)
            return Result<CustomerDto>.Failure("Khách hàng không tồn tại.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return Result<CustomerDto>.Failure("Tên khách hàng không được để trống.");
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            return Result<CustomerDto>.Failure("Số điện thoại không được để trống.");

        var phoneConflict = await _context.Customers.AnyAsync(c => c.PhoneNumber == dto.PhoneNumber.Trim() && c.Id != dto.Id && c.IsActive);
        if (phoneConflict)
            return Result<CustomerDto>.Failure($"Số điện thoại '{dto.PhoneNumber}' đã trùng với khách hàng khác.");

        customer.FullName = dto.FullName.Trim();
        customer.PhoneNumber = dto.PhoneNumber.Trim();
        customer.Email = dto.Email?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.Note = dto.Note;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (customer == null)
            return Result.Failure("Khách hàng không tồn tại.");

        var hasOrders = customer.Orders.Any(o => o.IsActive);
        if (hasOrders)
            return Result.Failure("Không thể xóa khách hàng đã có đơn hàng trong hệ thống.");

        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        Id = c.Id,
        CustomerCode = c.CustomerCode,
        FullName = c.FullName,
        PhoneNumber = c.PhoneNumber,
        Email = c.Email,
        Address = c.Address,
        TotalSpent = c.TotalSpent,
        RewardPoints = c.RewardPoints,
        Note = c.Note,
        TotalOrders = c.Orders.Count(o => o.IsActive),
        CreatedAt = c.CreatedAt
    };
}
