using Microsoft.EntityFrameworkCore;
using System.Reflection;
using UserActivityLogger.Extensions;

namespace UserActivityLogger.Tests.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureUserActivityLogger();
        //Register Configuration 
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
