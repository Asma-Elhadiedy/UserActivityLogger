using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UserActivityLogger.Extensions;

namespace UserActivityLogger.UnitTests;

public sealed class TestDbContext : DbContext, IDisposable
{
    private readonly SqliteConnection _connection;

    public bool FailOnSaveChangesAsync { get; set; }
    public Exception ExceptionToThrow { get; set; } = new DbUpdateException("Database connection failed during save.");

    public TestDbContext(DbContextOptions<TestDbContext> options, SqliteConnection connection)
        : base(options)
    {
        _connection = connection;
    }

    public static TestDbContext Create()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestDbContext(options, connection);
        context.Database.EnsureCreated();
        return context;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureUserActivityLogger();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<int>(cancellationToken);
        }

        if (FailOnSaveChangesAsync)
        {
            throw ExceptionToThrow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public override void Dispose()
    {
        base.Dispose();
        _connection.Dispose();
    }
}

