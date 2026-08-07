using Microsoft.EntityFrameworkCore;
using UserActivityLogger.Extensions;
using UserActivityLogger.Tests.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Add UserActivityLogger services
builder.Services.AddUserActivityLogger<ApplicationDbContext>(options =>
{
    options.EnableRequestBodyLogging = false;
    options.LogAnonymousUsers = true;
    options.RedactedFields = new[] { "password", "pwd", "secret" };
    options.SkipPaths = new[] { "/health", "/api/metrics" };

    //// Custom IP resolver for load balancer/proxy scenarios
    //options.IpAddressResolver = (httpContext) =>
    //{
    //    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    //    return !string.IsNullOrEmpty(forwardedFor)
    //        ? forwardedFor.Split(',')[0].Trim()
    //        : httpContext.Connection.RemoteIpAddress?.ToString();
    //};
});

// Register the filter with MVC
builder.Services.AddControllersWithViews(options =>
{
    options.AddUserActivityLogging(); // Add this line
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
