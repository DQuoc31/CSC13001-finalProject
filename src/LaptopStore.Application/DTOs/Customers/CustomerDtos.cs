namespace LaptopStore.Application.DTOs.Customers;

public class CustomerDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal TotalSpent { get; set; }
    public int RewardPoints { get; set; }
    public string? Note { get; set; }
    public int TotalOrders { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCustomerDto
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
}

public class UpdateCustomerDto : CreateCustomerDto
{
    public int Id { get; set; }
}
