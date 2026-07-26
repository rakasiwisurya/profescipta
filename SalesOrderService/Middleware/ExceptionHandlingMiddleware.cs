using System.Text.Json;
using SalesOrderService.Common;

namespace SalesOrderService.Middleware;

/// <summary>
/// Menangkap exception yang tidak tertangani dan mengubahnya menjadi
/// format error seragam sesuai FSD bagian 6:
/// { "success": false, "message": "...", "errors": [...] }
///
/// Dengan ini Front-End selalu menerima bentuk JSON yang sama, baik
/// untuk error validasi (400), tidak ditemukan (404), maupun error
/// tak terduga (500).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Pengguna/menutup browser membatalkan request — bukan error server.
            _logger.LogInformation("Request {Path} dibatalkan oleh pemanggil.", context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Terjadi error tak tertangani pada {Path}", context.Request.Path);

            if (context.Response.HasStarted)
            {
                // Header sudah terkirim, tidak bisa diubah lagi.
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            ApiResponse body = ApiResponse.Fail(
                "Terjadi kesalahan pada Sales Order Service.",
                new[] { ex.Message });

            await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
