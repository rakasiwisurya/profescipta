namespace SalesOrderService.Common;

/// <summary>
/// Jenis kegagalan yang mungkin terjadi di lapisan Service.
/// Controller memetakan ini ke HTTP status code:
/// Validation -> 400, NotFound -> 404.
/// </summary>
public enum ServiceErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2
}

/// <summary>
/// Hasil sebuah operasi bisnis. Dipakai supaya lapisan Service tidak
/// perlu tahu soal HTTP (tidak mengembalikan IActionResult) dan tidak
/// perlu memakai exception untuk alur yang normal seperti gagal validasi.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; protected init; }

    public ServiceErrorType ErrorType { get; protected init; }

    public string Message { get; protected init; } = string.Empty;

    public IReadOnlyList<string> Errors { get; protected init; } = Array.Empty<string>();

    public static ServiceResult Success(string message) =>
        new() { Succeeded = true, ErrorType = ServiceErrorType.None, Message = message };

    public static ServiceResult ValidationFailed(string message, IReadOnlyList<string> errors) =>
        new() { Succeeded = false, ErrorType = ServiceErrorType.Validation, Message = message, Errors = errors };

    public static ServiceResult NotFound(string message) =>
        new() { Succeeded = false, ErrorType = ServiceErrorType.NotFound, Message = message };
}

/// <summary>
/// Versi <see cref="ServiceResult"/> yang membawa data hasil operasi
/// (misalnya ID order baru atau detail order).
/// </summary>
public class ServiceResult<TData> : ServiceResult
{
    public TData? Data { get; private init; }

    public static ServiceResult<TData> Success(TData data, string message) =>
        new() { Succeeded = true, ErrorType = ServiceErrorType.None, Message = message, Data = data };

    public static new ServiceResult<TData> ValidationFailed(string message, IReadOnlyList<string> errors) =>
        new() { Succeeded = false, ErrorType = ServiceErrorType.Validation, Message = message, Errors = errors };

    public static new ServiceResult<TData> NotFound(string message) =>
        new() { Succeeded = false, ErrorType = ServiceErrorType.NotFound, Message = message };
}
