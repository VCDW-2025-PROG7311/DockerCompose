using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Client.Models;

namespace Client.Controllers;

public class HomeController : Controller
{
    private readonly HttpClient _httpClient;
    public HomeController(IHttpClientFactory clientFactory)
    {
        _httpClient = clientFactory.CreateClient("API");
    }

    public async Task<IActionResult> Index()
    {
        var forecasts = await _httpClient.GetFromJsonAsync<List<WeatherForecast>>("weatherforecast");
        return View(forecasts);
    }
}
