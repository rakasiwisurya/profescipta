using System.Text.Json.Serialization;
using CustomerService.Data;
using CustomerService.Middleware;
using CustomerService.Repositories;
using CustomerService.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Registrasi dependency (Controller -> Service -> Repository -> DB)
// ---------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Properti null tidak perlu ikut dikirim di respons JSON.
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Singleton: factory hanya menyimpan connection string, tidak menyimpan
// koneksi terbuka, jadi aman dipakai bersama antar request.
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

// Scoped: satu instance per HTTP request.
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerAppService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Customer Service",
        Version = "v1",
        Description = "Data master pelanggan untuk Sales Order Management System."
    });
});

var app = builder.Build();

// Middleware error harus dipasang paling awal agar bisa menangkap
// exception dari middleware/endpoint di bawahnya.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Customer Service v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

// Endpoint kecil untuk memastikan service hidup (dipakai saat cek manual).
app.MapGet("/health", () => Results.Ok(new { service = "CustomerService", status = "healthy" }));

app.Run();
