using Microsoft.EntityFrameworkCore;
using UserActivityLogger.Extensions;
using UserActivityLogger.Tests.Data;

namespace UserActivityLogger.Tests;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection")));


        builder.Services.AddUserActivityLogger<ApplicationDbContext>(options =>
        {
            options.EnableRequestBodyLogging = false;
            options.LogAnonymousUsers = true;
            options.RedactedFields = new[] { "password", "pwd", "secret" };
            options.SkipPaths = new[] { "/health", "/api/metrics" };
        });

        builder.Services.AddControllersWithViews(options => options.AddUserActivityLogging());

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
    }
}
