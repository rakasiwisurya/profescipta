using FrontEnd.ApiClients;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// PENTING: project Front-End tidak punya akses database sama sekali.
// Tidak ada connection string, tidak ada Dapper/EF di sini — semua data
// diambil dari service lewat HTTP (FSD bagian 2.3, "aturan keras").
// ---------------------------------------------------------------------
builder.Services.AddControllersWithViews();

string customerServiceBaseUrl = builder.Configuration["Services:CustomerServiceBaseUrl"]
    ?? throw new InvalidOperationException("Services:CustomerServiceBaseUrl belum diatur.");

string salesOrderServiceBaseUrl = builder.Configuration["Services:SalesOrderServiceBaseUrl"]
    ?? throw new InvalidOperationException("Services:SalesOrderServiceBaseUrl belum diatur.");

string? salesOrderApiKey = builder.Configuration["Services:SalesOrderApiKey"];

// Typed HttpClient: base URL diatur sekali di sini, sehingga kelas
// pemanggil (ApiClient) cukup menulis path relatif seperti "api/orders".
builder.Services.AddHttpClient<ICustomerApiClient, CustomerApiClient>(client =>
{
    client.BaseAddress = new Uri(customerServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ISalesOrderApiClient, SalesOrderApiClient>(client =>
{
    client.BaseAddress = new Uri(salesOrderServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);

    // API Key dipasang di satu tempat supaya endpoint yang diamankan
    // (DELETE /api/orders/{id} dan GET /api/orders/export) bisa dipanggil.
    if (!string.IsNullOrWhiteSpace(salesOrderApiKey))
    {
        client.DefaultRequestHeaders.Add("X-Api-Key", salesOrderApiKey);
    }
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

// Halaman utama aplikasi adalah Order List.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Orders}/{action=Index}/{id?}");

app.Run();
