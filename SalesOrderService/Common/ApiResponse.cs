namespace SalesOrderService.Common;

/// <summary>
/// Format respons seragam untuk semua service, sesuai FSD bagian 6:
/// { "success": false, "message": "...", "errors": ["...", "..."] }
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detail error per aturan bisnis yang dilanggar.
    /// Null saat sukses supaya tidak ikut muncul di JSON.
    /// </summary>
    public IReadOnlyList<string>? Errors { get; set; }

    public static ApiResponse Ok(string message) =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

/// <summary>
/// Respons khusus POST /api/orders yang juga membawa ID order baru
/// (FSD: { "success": true, "salesSoId": 123, "message": "..." }).
/// </summary>
public class CreateOrderResponse : ApiResponse
{
    public int SalesSoId { get; set; }
}
