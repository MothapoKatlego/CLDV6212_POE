using KatRetailStore.Data;
using KatRetailStore.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add configuration sources
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// In Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()
    ));

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register Azure SQL Database


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AzureSqlConnection")));

// Register your services
try
{
    builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

    // Test configuration
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage");
    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("WARNING: AzureStorage connection string is not configured!");
    }
    else
    {
        Console.WriteLine("AzureStorage connection string found and configured.");
    }

    var sqlConnectionString = builder.Configuration.GetConnectionString("AzureSqlConnection");
    if (string.IsNullOrEmpty(sqlConnectionString))
    {
        Console.WriteLine("WARNING: AzureSqlConnection connection string is not configured!");
    }
    else
    {
        Console.WriteLine("AzureSqlConnection connection string found and configured.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR configuring services: {ex.Message}");
    throw;
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var storageService = services.GetRequiredService<IAzureStorageService>();

        await DataSeeder.Initialize(context, storageService);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error seeding data: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ADD THIS - Manual browser launch in development
if (app.Environment.IsDevelopment())
{
    // Get the application URL
    var urls = app.Urls;
    var applicationUrl = "https://localhost:7000"; // Default, adjust if different

    Console.WriteLine($"Application starting...");
    Console.WriteLine($"Please manually open: {applicationUrl}");

    // Try to launch browser
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = applicationUrl,
            UseShellExecute = true
        });
        Console.WriteLine($"Browser launched successfully to: {applicationUrl}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not launch browser automatically: {ex.Message}");
        Console.WriteLine($"Please manually navigate to: {applicationUrl}");
    }
}

// Add a simple test endpoint
app.MapGet("/test", () => "Web server is running! Database connections are configured.");

Console.WriteLine("Web server started. Press Ctrl+C to stop.");

app.MapGet("/db-test", async (ApplicationDbContext context) =>
{
    try
    {
        var productCount = await context.Products.CountAsync();
        return Results.Ok(new
        {
            Status = "✅ Database Connection Successful!",
            ProductCount = productCount,
            Message = $"Found {productCount} products in Azure SQL Database"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"❌ Database Connection Failed: {ex.Message}");
    }
});
app.Run();