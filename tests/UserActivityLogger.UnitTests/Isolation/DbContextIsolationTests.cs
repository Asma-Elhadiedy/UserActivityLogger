using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserActivityLogger.Entities;
using UserActivityLogger.Extensions;
using UserActivityLogger.Options;
using UserActivityLogger.Services;
using UserActivityLogger.UnitTests.Models;
using Xunit;

namespace UserActivityLogger.UnitTests.Isolation;

public class DbContextIsolationTests : IDisposable
{
    private readonly List<SqliteConnection> _connections = new();

    private SqliteConnection CreateOpenConnection(string dbName)
    {
        var connection = new SqliteConnection($"Data Source=Test_{dbName};Mode=Memory;Cache=Shared");
        connection.Open();
        _connections.Add(connection);
        return connection;
    }

    private IServiceProvider BuildServiceProvider(
        string databaseName,
        Action<UserActivityLoggerOptions>? configureOptions = null,
        bool useDedicated = false)
    {
        var connection = CreateOpenConnection(databaseName);

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/orders";
        httpContext.Request.Method = "POST";
        httpContext.Response.StatusCode = 200;
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") },
            "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        if (useDedicated)
        {
            services.AddDedicatedUserActivityLogger(
                options => options.UseSqlite(connection),
                configureOptions);

            var sp = services.BuildServiceProvider();
            using var initContext = new AuditDbContext(new DbContextOptionsBuilder<AuditDbContext>().UseSqlite(connection).Options);
            initContext.Database.EnsureCreated();
            return sp;
        }
        else
        {
            services.AddDbContext<TestApplicationDbContext>(options =>
                options.UseSqlite(connection));

            services.AddUserActivityLogger<TestApplicationDbContext>(configureOptions);

            var sp = services.BuildServiceProvider();
            using var initContext = new TestApplicationDbContext(new DbContextOptionsBuilder<TestApplicationDbContext>().UseSqlite(connection).Options);
            initContext.Database.EnsureCreated();
            return sp;
        }
    }

    [Fact]
    public async Task LogActionAsync_WhenIsolatePersistenceIsTrue_DoesNotCommitPendingBusinessEntities()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var provider = BuildServiceProvider(dbName, options =>
        {
            options.IsolatePersistence = true;
        });

        using var scope = provider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<TestApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<IUserActivityLogger>();

        // Stage an uncommitted business entity in the application's scoped DbContext
        var customer = new Customer { Id = 10, Name = "Alice" };
        scopedContext.Customers.Add(customer);

        Assert.Equal(EntityState.Added, scopedContext.Entry(customer).State);

        // Act: log user activity with isolated persistence enabled
        await logger.LogActionAsync("CustomerOrderPlaced", new { OrderId = 99 });

        // Assert:
        // 1. Customer in the scoped context must remain uncommitted (EntityState.Added)
        Assert.Equal(EntityState.Added, scopedContext.Entry(customer).State);

        // 2. Querying a separate context verifies Customer was NOT saved to the database
        using var verificationScope = provider.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<TestApplicationDbContext>();

        var savedCustomer = await verificationContext.Customers.FindAsync(10);
        Assert.Null(savedCustomer);

        // 3. The audit log WAS saved to the database by the isolated context
        var logs = await verificationContext.UserActivityLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("CustomerOrderPlaced", logs[0].Event);
        Assert.Equal("user-123", logs[0].UserId);
    }

    [Fact]
    public async Task LogActionAsync_WhenIsolatePersistenceIsFalse_CommitsPendingBusinessEntitiesInSharedContext()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var provider = BuildServiceProvider(dbName, options =>
        {
            options.IsolatePersistence = false;
        });

        using var scope = provider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<TestApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<IUserActivityLogger>();

        // Stage an uncommitted business entity
        var customer = new Customer { Id = 20, Name = "Bob" };
        scopedContext.Customers.Add(customer);

        Assert.Equal(EntityState.Added, scopedContext.Entry(customer).State);

        // Act: log user activity in legacy shared mode
        await logger.LogActionAsync("LegacySharedLogAction");

        // Assert: Customer was committed because SaveChangesAsync was called on the shared context
        Assert.Equal(EntityState.Unchanged, scopedContext.Entry(customer).State);

        using var verificationScope = provider.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<TestApplicationDbContext>();

        var savedCustomer = await verificationContext.Customers.FindAsync(20);
        Assert.NotNull(savedCustomer);
        Assert.Equal("Bob", savedCustomer.Name);

        var logs = await verificationContext.UserActivityLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("LegacySharedLogAction", logs[0].Event);
    }

    [Fact]
    public void AddIsolatedUserActivityLogger_EnablesIsolatePersistenceByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestApplicationDbContext>(o => o.UseSqlite("Filename=:memory:"));

        services.AddIsolatedUserActivityLogger<TestApplicationDbContext>();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<UserActivityLoggerOptions>();

        Assert.True(options.IsolatePersistence);
    }

    [Fact]
    public async Task AddDedicatedUserActivityLogger_PersistsAuditLogsToDedicatedAuditDbContext()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var provider = BuildServiceProvider(dbName, useDedicated: true);

        using var scope = provider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IUserActivityLogger>();

        await logger.LogActionAsync("DedicatedLogEvent", new { Meta = "AuditOnly" });

        var factory = provider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var verificationContext = await factory.CreateDbContextAsync();

        var logs = await verificationContext.UserActivityLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("DedicatedLogEvent", logs[0].Event);
        Assert.Equal("user-123", logs[0].UserId);
    }

    [Fact]
    public async Task AddDedicatedUserActivityLogger_WithServiceProviderOverload_PersistsSuccessfully()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var connection = CreateOpenConnection(dbName);

        var services = new ServiceCollection();
        services.AddLogging();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/test";
        httpContext.Request.Method = "GET";
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        services.AddDedicatedUserActivityLogger((sp, builder) =>
        {
            builder.UseSqlite(connection);
        });

        var provider = services.BuildServiceProvider();

        using (var initContext = new AuditDbContext(new DbContextOptionsBuilder<AuditDbContext>().UseSqlite(connection).Options))
        {
            initContext.Database.EnsureCreated();
        }

        using var scope = provider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IUserActivityLogger>();

        await logger.LogActionAsync("SpOverloadEvent");

        var factory = provider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var verificationContext = await factory.CreateDbContextAsync();

        var logs = await verificationContext.UserActivityLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("SpOverloadEvent", logs[0].Event);
    }

    [Fact]
    public async Task LogActionAsync_WhenAnonymousUsersDisabled_AndNoUser_DoesNotPersistLog()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var connection = CreateOpenConnection(dbName);

        var services = new ServiceCollection();
        services.AddLogging();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/anon";
        httpContext.Request.Method = "GET";
        httpContext.User = new ClaimsPrincipal(); // anonymous
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        services.AddDbContext<TestApplicationDbContext>(o => o.UseSqlite(connection));
        services.AddUserActivityLogger<TestApplicationDbContext>(options =>
        {
            options.LogAnonymousUsers = false;
        });

        var provider = services.BuildServiceProvider();

        using (var initContext = new TestApplicationDbContext(new DbContextOptionsBuilder<TestApplicationDbContext>().UseSqlite(connection).Options))
        {
            initContext.Database.EnsureCreated();
        }

        using var scope = provider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IUserActivityLogger>();

        await logger.LogActionAsync("AnonAction");

        var db = scope.ServiceProvider.GetRequiredService<TestApplicationDbContext>();
        var logs = await db.UserActivityLogs.ToListAsync();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task LogAsync_WhenSharedContextFails_DetachesAuditLogEntryFromChangeTracker()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var connection = CreateOpenConnection(dbName);

        var services = new ServiceCollection();
        services.AddLogging();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        services.AddDbContext<FailingApplicationDbContext>(o => o.UseSqlite(connection));
        services.AddUserActivityLogger<FailingApplicationDbContext>(o => o.IsolatePersistence = false);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FailingApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<IUserActivityLogger>();

        var log = new UserActivityLog { Event = "FailTest" };
        await logger.LogAsync(log);

        // After failure, the audit log entry must be detached so it doesn't corrupt subsequent operations
        var entry = context.Entry(log);
        Assert.Equal(EntityState.Detached, entry.State);
    }

    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            connection.Dispose();
        }
        _connections.Clear();
    }
}
