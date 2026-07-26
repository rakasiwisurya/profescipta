namespace FrontEnd.Models;

/// <summary>
/// Hasil pemanggilan API oleh front-end.
///
/// Front-end tidak pernah menyusun pesan error sendiri: pesan yang
/// ditampilkan ke pengguna selalu diambil dari respons service
/// (FSD bagian 5: "Front-end hanya menampilkan pesan error atau nilai
/// yang dikembalikan oleh service").
/// </summary>
public class ApiResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>True kalau service membalas 404.</summary>
    public bool NotFound { get; init; }

    public static ApiResult Ok(string message) =>
        new() { Success = true, Message = message };

    public static ApiResult Fail(string message, IReadOnlyList<string>? errors = null, bool notFound = false) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? Array.Empty<string>(),
            NotFound = notFound
        };
}

/// <summary>Versi <see cref="ApiResult"/> yang membawa data hasil pemanggilan.</summary>
public class ApiResult<TData> : ApiResult
{
    public TData? Data { get; init; }

    public static ApiResult<TData> Ok(TData data, string message = "") =>
        new() { Success = true, Message = message, Data = data };

    public static new ApiResult<TData> Fail(
        string message, IReadOnlyList<string>? errors = null, bool notFound = false) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? Array.Empty<string>(),
            NotFound = notFound
        };
}

/// <summary>
/// Bentuk respons standar dari service (FSD bagian 6):
/// { "success": ..., "message": "...", "errors": [...] }
/// </summary>
public class ServiceEnvelope
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public List<string>? Errors { get; set; }

    public int? SalesSoId { get; set; }
}
