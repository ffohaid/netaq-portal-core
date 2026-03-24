namespace Netaq.Application.Common.Models;

/// <summary>
/// Unified API response wrapper: { isSuccess: bool, data: T, error: string }
/// </summary>
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    public static ApiResponse<T> Success(T data, string _message) => new()
    {
        IsSuccess = true,
        Data = data
    };

    public static ApiResponse<T> Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    public static ApiResponse<T> Failure(List<string> errors) => new()
    {
        IsSuccess = false,
        Errors = errors,
        Error = errors.FirstOrDefault()
    };

    // Aliases for backward compatibility
    public static ApiResponse<T> Ok(T data) => Success(data);
    public static ApiResponse<T> Fail(string error) => Failure(error);
}

/// <summary>
/// Paginated response for list endpoints.
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Paginated result wrapper for query handlers.
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
