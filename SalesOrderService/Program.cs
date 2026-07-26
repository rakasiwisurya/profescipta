using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SalesOrderService.Common;
using SalesOrderService.Data;
using SalesOrderService.Domain;
using SalesOrderService.Middleware;
using SalesOrderService.Repositories;
using SalesOrderService.Security;
using SalesOrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Registrasi dependency (Controller -> Service -> Repository -> DB)
// ---------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Properti null tidak perlu ikut dikirim, supaya bentuk respons
        // sukses persis seperti contoh di FSD (tanpa "errors": null).
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Menyeragamkan error hasil model binding (misal JSON qty berisi teks)
// agar tetap memakai format { success, message, errors } seperti error
// validasi bisnis, bukan format ProblemDetails bawaan ASP.NET.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        string[] errors = context.ModelState
            .SelectMany(entry => entry.Value!.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? "Format data tidak valid."
                : error.ErrorMessage)
            .ToArray();

        return new BadRequestObjectResult(
            ApiResponse.Fail("Format request tidak valid", errors));
    };
});

builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

// Kelas domain tidak menyimpan state apa pun, jadi cukup satu instance.
builder.Services.AddSingleton<OrderValidator>();
builder.Services.AddSingleton<OrderCalculator>();
builder.Services.AddSingleton<IOrderExcelExporter, ClosedXmlOrderExcelExporter>();

builder.Services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sales Order Service",
        Version = "v1",
        Description = "CRUD Sales Order, pencarian, kalkulasi total, dan ekspor Excel."
    });

    // Supaya endpoint yang diamankan bisa dicoba langsung dari Swagger UI
    // (tombol Authorize -> masukkan API Key).
    options.AddSecurityDefinition(ApiKeyAuthorizeAttribute.ApiKeyHeaderName, new OpenApiSecurityScheme
    {
        Name = ApiKeyAuthorizeAttribute.ApiKeyHeaderName,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API Key untuk endpoint DELETE /api/orders/{id} dan GET /api/orders/export."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = ApiKeyAuthorizeAttribute.ApiKeyHeaderName
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sales Order Service v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { service = "SalesOrderService", status = "healthy" }));

app.Run();
