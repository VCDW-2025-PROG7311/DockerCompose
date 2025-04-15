using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Client.Models;

namespace Client.Controllers;

public class HomeController : Controller
{
    private readonly HttpClient _http;

    public HomeController(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("API");
    }

    public async Task<IActionResult> Index()
    {
        var forecasts = await _http.GetFromJsonAsync<List<WeatherForecast>>("weatherforecast");
        return View(forecasts);
    }

    [HttpPost]
    public async Task<IActionResult> Generate()
    {
        await _http.PostAsync("weatherforecast/generate", null);
        return RedirectToAction("Index");
    }
}
