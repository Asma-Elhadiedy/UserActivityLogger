using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UserActivityLogger.Entities;
using UserActivityLogger.Extensions;

namespace UserActivityLogger.UnitTests.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TestApplicationDbContext : DbContext
{
    public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureUserActivityLogger();
    }
}

public class FailingApplicationDbContext : DbContext
{
    public FailingApplicationDbContext(DbContextOptions<FailingApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureUserActivityLogger();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new DbUpdateException("Simulated save failure");
    }
}
