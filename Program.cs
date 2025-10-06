using KatRetailStore.Services;
using KatRetailStore.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services for both MVC and API
builder.Services.AddControllersWithViews();  // For MVC Views
builder.Services.AddControllers();           // For API Controllers

// Register your services
builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Map both MVC routes and API controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();  // This maps your API controllers

app.Run();