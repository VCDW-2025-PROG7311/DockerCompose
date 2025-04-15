using API.Models;
using API.Data;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80); // ← This forces it to bind correctly inside Docker
});

var connectionString = builder.Configuration["DB_CONNECTION_STRING"];

builder.Services.AddDbContext<ForecastDbContext>(options =>
    options.UseSqlServer(connectionString,
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
    )
);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", async (ForecastDbContext db) =>
{
    return await db.WeatherForecasts.OrderByDescending(f => f.Date).ToListAsync();
});

app.MapPost("/weatherforecast/generate", async (ForecastDbContext db) =>
{
    var summaries = new[] { "Freezing", "Chilly", "Mild", "Warm", "Hot" };

    var forecasts = Enumerable.Range(1, 5).Select(i =>
        new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        }).ToList();

    await db.WeatherForecasts.AddRangeAsync(forecasts);
    await db.SaveChangesAsync();

    return Results.Ok(forecasts);
});

app.Run();