## Dockerizing the Weather Forecast App (API + Client + SQL Server)

Pre-requisite knowledge: This guide assumes you already have the app Dockerized with an API and client. If not, [see here](../Guides//DockerizingWithoutSql.md).

This guide walks you through setting up a full-stack .NET app using Docker Compose. You will:
- Set up a SQL Server database in a container
- Manually create the `DbContext` and model
- Dockerize both the API and client
- Use environment variables for secrets


### Folder Structure

```
/DockerComposeRoot
├── API/
│   ├── Dockerfile
│   ├── Program.cs
│   └── Data/ForecastDbContext.cs
│   └── Models/WeatherForecast.cs
├── Client/
│   ├── Dockerfile
│   └── Views/Home/Index.cshtml
├── db-init/
│   ├── init.sql
├── .env
└── docker-compose.yml
```

## Create `.env` file (same level as `docker-compose.yml`)

```env
DB_SA_PASSWORD=YourStrong@123
API_DB_CONNECTION_STRING=Server=db;Database=Weather_DB;User Id=sa;Password=YourStrong@123;TrustServerCertificate=True;
```

Add `.env` to `.gitignore` so it's not committed:
```
.env
```

---

## Update `docker-compose.yml`

```yaml
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: db
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: ${DB_SA_PASSWORD}
    ports:
      - "1433:1433"
    networks:
      - net
    volumes:
      - dbdata:/var/opt/mssql
    healthcheck:
      test: ["CMD", "bash", "-c", "echo > /dev/tcp/localhost/1433"]
      interval: 10s
      timeout: 5s
      retries: 10

  db-setup:
    image: mcr.microsoft.com/mssql-tools
    depends_on:
      db:
        condition: service_healthy
    entrypoint: >
      bash -c "
        /opt/mssql-tools/bin/sqlcmd -S db -U sa -P ${DB_SA_PASSWORD} -i /scripts/init.sql
      "
    volumes:
      - ./db-init:/scripts
    networks:
      - net

  api:
    build:
      context: ./API
      dockerfile: Dockerfile
    container_name: api
    depends_on:
      - db-setup
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      DB_CONNECTION_STRING: ${API_DB_CONNECTION_STRING}
    ports:
      - "5000:80"
    networks:
      - net

  client:
    build:
      context: ./Client
      dockerfile: Dockerfile
    container_name: client
    depends_on:
      - api
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      API_BASE_URL: http://api
    ports:
      - "5001:80"
    networks:
      - net

networks:
  net:

volumes:
  dbdata:

```

### Add in `db-init/init.sql`

```sql
IF DB_ID('Weather_DB') IS NULL
BEGIN
    CREATE DATABASE Weather_DB;
END
GO

USE Weather_DB;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WeatherForecasts' AND xtype='U')
BEGIN
    CREATE TABLE WeatherForecasts (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Date DATE NOT NULL,
        TemperatureC INT NOT NULL,
        Summary NVARCHAR(100) NULL
    );
END
GO
```

### Create `ForecastDbContext.cs` manually

`API/Data/ForecastDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Data
{
    public class ForecastDbContext : DbContext
    {
        public ForecastDbContext(DbContextOptions<ForecastDbContext> options)
            : base(options) { }

        public DbSet<WeatherForecast> WeatherForecasts { get; set; }
    }
}
```

### Create `WeatherForecast.cs` manually

📄 `API/Models/WeatherForecast.cs`

```csharp
namespace API.Models
{
    public class WeatherForecast
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }
        public string? Summary { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
```

---

### Edit API `Program.cs`

```csharp
var connectionString = builder.Configuration["DB_CONNECTION_STRING"];

builder.Services.AddDbContext<ForecastDbContext>(options =>
    options.UseSqlServer(connectionString,
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
    )
);
```

### Update Client Controller (Index + Generate Forecasts)

```csharp
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
```

### Run the App

```bash
docker compose down -v
docker compose build --no-cache
docker compose up
```

- Browse to http://localhost:5001 to see the client
- The client connects to the API
- The API talks to SQL Server to save + view forecasts

## GitHub Actions: Docker Compose Build & Init

This GitHub Action workflow automates the build and execution of a multi-container setup using Docker Compose.

### Workflow File
Add a new yml file to this location. Call it build.yml
```
.github/workflows/build.yml
```
### Trigger
This workflow runs automatically on:

- `push` to the `sql` branch
- `pull_request` to the `sql` branch

### Add Required Secrets

Before using this workflow, set the following secrets in your GitHub repo:

| Secret Name            | Description                             |
|------------------------|-----------------------------------------|
| `DB_SA_PASSWORD`       | YourStrong@123              |
| `API_DB_CONNECTION_STRING` | Server=db;Database=Weather_DB;User Id=sa;Password=YourStrong@123;TrustServerCertificate=True; |

### Workflow Definition

```yaml
name: Docker Compose Build and Init Pipeline

on:
  push:
    branches: [ "sql" ]
  pull_request:
    branches: [ "sql" ]

jobs:
  docker-compose:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout Code
      uses: actions/checkout@v3

    - name: Set up Docker Compose
      run: |
        sudo apt-get update
        sudo apt-get install docker-compose -y

    - name: Create .env file
      run: |
        echo "DB_SA_PASSWORD=${{ secrets.DB_SA_PASSWORD }}" >> .env
        echo "API_DB_CONNECTION_STRING=${{ secrets.API_DB_CONNECTION_STRING }}" >> .env

    - name: Run Docker Compose
      run: docker-compose -f docker-compose.yml up --build --abort-on-container-exit

    - name: Shut down Docker Compose
      if: always()
      run: docker-compose -f docker-compose.yml down -v
```
