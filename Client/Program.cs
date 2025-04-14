var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["API_BASE_URL"] ?? "http://localhost:5000";

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80); // ← This forces it to bind correctly inside Docker
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
