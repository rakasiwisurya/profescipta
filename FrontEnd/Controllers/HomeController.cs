using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Controllers;

/// <summary>
/// Controller pendukung: mengarahkan halaman utama ke Order List dan
/// menampilkan halaman error umum.
/// </summary>
public class HomeController : Controller
{
    /// <summary>Halaman utama aplikasi adalah Order List.</summary>
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Orders");

    /// <summary>
    /// Halaman error umum (dipakai UseExceptionHandler saat mode Production).
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
