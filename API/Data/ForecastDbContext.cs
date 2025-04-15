using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Data
{
    public class ForecastDbContext : DbContext
    {
        public ForecastDbContext(DbContextOptions<ForecastDbContext> options) : base(options) { }

                public DbSet<WeatherForecast> WeatherForecasts { get; set; } // ✅ matches public model

    }
}