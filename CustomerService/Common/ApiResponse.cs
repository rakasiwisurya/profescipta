namespace CustomerService.Common;

/// <summary>
/// Format respons error seragam untuk semua service, sesuai FSD bagian 6:
/// { "success": false, "message": "...", "errors": ["...", "..."] }
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detail error per field. Dikosongkan (null) saat sukses supaya
    /// tidak muncul di JSON respons.
    /// </summary>
    public IReadOnlyList<string>? Errors { get; set; }

    public static ApiResponse Ok(string message) =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
