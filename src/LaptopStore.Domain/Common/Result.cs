namespace LaptopStore.Domain.Common;

/// <summary>
/// Đại diện cho kết quả của một thao tác nghiệp vụ, hỗ trợ xử lý lỗi rõ ràng không cần lạm dụng Exception.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && !string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("Thành công thì không được có thông báo lỗi.");
        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("Thất bại thì bắt buộc phải có thông báo lỗi.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

/// <summary>
/// Kết quả trả về kèm dữ liệu kiểu T.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Không thể truy cập Value khi thao tác thất bại: " + Error);

    protected Result(T? value, bool isSuccess, string? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null);
    public static new Result<T> Failure(string error) => new(default, false, error);
}
