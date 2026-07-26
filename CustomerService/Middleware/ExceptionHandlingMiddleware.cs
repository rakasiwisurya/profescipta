using System.Text.Json;
using CustomerService.Common;

namespace CustomerService.Middleware;

/// <summary>
/// Menangkap exception yang tidak tertangani di seluruh service dan
/// mengubahnya menjadi format error seragam sesuai FSD bagian 6:
/// { "success": false, "message": "...", "errors": [...] }
///
/// Tujuannya: Front-End selalu menerima bentuk JSON yang sama,
/// baik error validasi maupun error tak terduga.
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Terjadi error tak tertangani pada {Path}", context.Request.Path);

            // Kalau respons sudah mulai dikirim, header tidak bisa diubah lagi.
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            ApiResponse body = ApiResponse.Fail(
                "Terjadi kesalahan pada Customer Service.",
                new[] { ex.Message });

            await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
