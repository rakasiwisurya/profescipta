using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SalesOrderService.Common;

namespace SalesOrderService.Security;

/// <summary>
/// Pengamanan endpoint dengan API Key (FSD bagian 7.4 — nilai plus).
///
/// Cara pakai: tempelkan [ApiKeyAuthorize] pada action yang mau diamankan.
/// Pemanggil wajib mengirim header:  X-Api-Key: &lt;kunci&gt;
/// Kunci yang sah dibaca dari konfigurasi "Security:ApiKey".
///
/// Dipakai pada endpoint yang berdampak/destruktif:
///   DELETE /api/orders/{id}   dan   GET /api/orders/export
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    public const string ApiKeyHeaderName = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        IConfiguration configuration =
            context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        string? expectedApiKey = configuration["Security:ApiKey"];

        // Kalau server belum dikonfigurasi, tolak — lebih aman daripada
        // membiarkan endpoint terbuka tanpa kunci.
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            context.Result = new ObjectResult(ApiResponse.Fail(
                "API Key belum dikonfigurasi di server.",
                new[] { "Isi konfigurasi Security:ApiKey pada Sales Order Service." }))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
        {
            context.Result = new ObjectResult(ApiResponse.Fail(
                "Akses ditolak.",
                new[] { $"Header {ApiKeyHeaderName} wajib dikirim untuk endpoint ini." }))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        if (!IsMatch(providedApiKey.ToString(), expectedApiKey))
        {
            context.Result = new ObjectResult(ApiResponse.Fail(
                "Akses ditolak.",
                new[] { "API Key tidak valid." }))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        // Kunci benar -> lanjutkan ke action aslinya.
        await next();
    }

    /// <summary>
    /// Membandingkan kunci dengan FixedTimeEquals (perbandingan waktu tetap)
    /// supaya lamanya proses tidak membocorkan seberapa banyak karakter
    /// yang sudah benar (timing attack).
    /// </summary>
    private static bool IsMatch(string providedApiKey, string expectedApiKey)
    {
        byte[] providedBytes = Encoding.UTF8.GetBytes(providedApiKey);
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);

        if (providedBytes.Length != expectedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
