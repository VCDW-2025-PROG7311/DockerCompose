
## Dockerizing MathApp with Docker Compose
We will now containerize a simple default weather forecast API and a client for it, and run them together using Docker Compose. 

### Prerequisites
1. Docker Desktop must be installed and running.
2. VSCode Docker Extension is recommended.
3. Your project folder structure should look like this:

```
/DockerCompose
├── API/
│   └── API.csproj
├── Client/
│   └── Client.csproj
├── docker-compose.yml
└── .gitignore
```

### Step 1: Create Dockerfiles
In both the `API/` and `Client/` folders, create a `Dockerfile` with no file extension.

Paste the following into each:

**For API/Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
```

**For Client/Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Client.dll"]
```

### Step 2: Create the `docker-compose.yml` file

At the root (next to API and Client), create a file named `docker-compose.yml`:

```yaml
version: '3.8'

services:
  mathapi:
    build:
      context: ./API
      dockerfile: Dockerfile
    container_name: mathapi
    environment:
      ASPNETCORE_ENVIRONMENT: Development
    ports:
      - "5000:80"
    networks:
      - mathnet

  mathapiclient:
    build:
      context: ./Client
      dockerfile: Dockerfile
    container_name: mathapiclient
    depends_on:
      - mathapi
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      API_BASE_URL: http://mathapi
    ports:
      - "5001:80"
    networks:
      - mathnet

networks:
  mathnet:
```

### Step 3: Fix Program.cs if needed

In both API and Client, **remove `UseHttpsRedirection()`** to avoid HTTPS conflicts in containers.

**Optional (recommended):** Add this line in `API/Program.cs` before `app.Run();`:
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);
});
```
### Step 4: Build and Run

From your root folder (where `docker-compose.yml` is):

```bash
docker compose build
docker compose up
```

To stop containers:

```bash
docker compose down
```

Step 5: Test

- `http://localhost:5000/weatherforecast` → API response
- `http://localhost:5001/` → Your client consuming the API


### Troubleshooting Tips

| Problem | Solution |
|--------|----------|
| `ERR_EMPTY_RESPONSE` | Remove `UseHttpsRedirection()`. Make sure your app binds to port 80 |
| API not reachable | Check if the container is running using `docker ps`. Look at logs with `docker compose logs mathapi` |
| Client can’t call API | Ensure `API_BASE_URL=http://mathapi` is set in Docker Compose |

---

### Next Steps

1. Add SQL Server as a third service in `docker-compose.yml`.
2. Update your connection string using `host.docker.internal`.
3. Add volume mounting and bind keys if using session-based features.
4. Dockerize **MathAPI** and ***MathAPIClient**
