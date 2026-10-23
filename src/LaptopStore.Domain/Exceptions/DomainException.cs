namespace LaptopStore.Domain.Exceptions;

/// <summary>
/// Ngoại lệ đại diện cho các vi phạm quy tắc nghiệp vụ trong Domain.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
