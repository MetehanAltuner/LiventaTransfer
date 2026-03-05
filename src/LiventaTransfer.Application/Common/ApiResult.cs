namespace LiventaTransfer.Application.Common;

public class ApiResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string TraceId { get; set; } = Guid.NewGuid().ToString();

    public static ApiResult<T> Ok(T data, string message = "", int statusCode = 200) =>
        new()
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = statusCode
        };

    public static ApiResult<T> Fail(string message, List<string>? errors = null, int statusCode = 400) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors,
            StatusCode = statusCode
        };
}

public sealed class PagedResult<TItem>
{
    public IReadOnlyList<TItem> Items { get; init; } = Array.Empty<TItem>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
