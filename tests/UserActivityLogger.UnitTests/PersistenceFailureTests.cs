using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UserActivityLogger.Entities;
using UserActivityLogger.Options;
using UserActivityLogger.Services;
using Xunit;

namespace UserActivityLogger.UnitTests;

public sealed class PersistenceFailureTests
{
    private readonly IHttpContextAccessor _httpContextAccessor = new HttpContextAccessor();

    [Fact]
    public async Task LogAsync_WhenPersistenceSucceeds_PersistsEntitySuccessfully()
    {
        using var context = TestDbContext.Create();
        var options = new UserActivityLoggerOptions();
        var service = new UserActivityLoggerService<TestDbContext>(
            NullLogger<UserActivityLoggerService<TestDbContext>>.Instance,
            context,
            _httpContextAccessor,
            options);

        var log = new UserActivityLog
        {
            Event = "User viewed report",
            Path = "/reports/1",
            Method = "GET",
            ResponseStatusCode = 200,
            DateEvent = DateTime.UtcNow
        };

        await service.LogAsync(log);

        var saved = await context.Set<UserActivityLog>().FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("User viewed report", saved.Event);
        Assert.Equal(EntityState.Unchanged, context.Entry(saved).State);
    }

    [Fact]
    public async Task LogAsync_WhenPersistenceFails_AndFailureBehaviorIsLogAndContinue_DoesNotThrowAndDetachesEntity()
    {
        using var context = TestDbContext.Create();
        context.FailOnSaveChangesAsync = true;
        var options = new UserActivityLoggerOptions
        {
            FailureBehavior = AuditFailureBehavior.LogAndContinue
        };

        var service = new UserActivityLoggerService<TestDbContext>(
            NullLogger<UserActivityLoggerService<TestDbContext>>.Instance,
            context,
            _httpContextAccessor,
            options);

        var log = new UserActivityLog
        {
            Event = "User created invoice",
            Path = "/invoices",
            Method = "POST",
            ResponseStatusCode = 201
        };

        // Act - should not throw
        await service.LogAsync(log);

        // Assert - entity must be detached from change tracker to prevent tracker poisoning
        Assert.Equal(EntityState.Detached, context.Entry(log).State);

        // Verify context change tracker is clean and can perform subsequent operations
        context.FailOnSaveChangesAsync = false;
        var pendingCount = context.ChangeTracker.Entries().Count();
        Assert.Equal(0, pendingCount);
    }

    [Fact]
    public async Task LogAsync_WhenPersistenceFails_AndFailureBehaviorIsThrow_RethrowsExceptionAndDetachesEntity()
    {
        using var context = TestDbContext.Create();
        var expectedException = new DbUpdateException("Simulated constraint violation.");
        context.FailOnSaveChangesAsync = true;
        context.ExceptionToThrow = expectedException;

        var options = new UserActivityLoggerOptions
        {
            FailureBehavior = AuditFailureBehavior.Throw
        };

        var service = new UserActivityLoggerService<TestDbContext>(
            NullLogger<UserActivityLoggerService<TestDbContext>>.Instance,
            context,
            _httpContextAccessor,
            options);

        var log = new UserActivityLog
        {
            Event = "User deleted customer",
            Path = "/customers/5",
            Method = "DELETE",
            ResponseStatusCode = 204
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => service.LogAsync(log));
        Assert.Same(expectedException, ex);

        // Entity must be detached even when rethrowing
        Assert.Equal(EntityState.Detached, context.Entry(log).State);
    }

    [Fact]
    public async Task TryLogAsync_WhenPersistenceFails_ReturnsFailedResultAndDetachesEntity()
    {
        using var context = TestDbContext.Create();
        var expectedException = new DbUpdateException("Simulated deadlock exception.");
        context.FailOnSaveChangesAsync = true;
        context.ExceptionToThrow = expectedException;

        var options = new UserActivityLoggerOptions();
        var service = new UserActivityLoggerService<TestDbContext>(
            NullLogger<UserActivityLoggerService<TestDbContext>>.Instance,
            context,
            _httpContextAccessor,
            options);

        var log = new UserActivityLog
        {
            Event = "User updated profile",
            Path = "/profile",
            Method = "PUT",
            ResponseStatusCode = 200
        };

        // Act
        var result = await service.TryLogAsync(log);

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(result.Canceled);
        Assert.Same(expectedException, result.Error);
        Assert.Equal(EntityState.Detached, context.Entry(log).State);
    }

    [Fact]
    public async Task TryLogAsync_WhenCancellationRequested_ReturnsCancelledResultAndDetachesEntity()
    {
        using var context = TestDbContext.Create();
        var options = new UserActivityLoggerOptions();
        var service = new UserActivityLoggerService<TestDbContext>(
            NullLogger<UserActivityLoggerService<TestDbContext>>.Instance,
            context,
            _httpContextAccessor,
            options);

        var log = new UserActivityLog
        {
            Event = "User export large dataset",
            Path = "/export",
            Method = "GET"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await service.TryLogAsync(log, cts.Token);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Canceled);
        Assert.IsAssignableFrom<OperationCanceledException>(result.Error);
        Assert.Equal(EntityState.Detached, context.Entry(log).State);
    }

    [Fact]
    public async Task LogAsync_WhenCancellationRequested_RethrowsOperationCanceledException()
    {
        using var context = TestDbContext.Create();
        var options = new UserActivityLoggerOptions();
        var service = new UserActivityLoggerService<TestDbContext>(
            NullLogger<UserActivityLoggerService<TestDbContext>>.Instance,
            context,
            _httpContextAccessor,
            options);

        var log = new UserActivityLog
        {
            Event = "User download file",
            Path = "/download",
            Method = "GET"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.LogAsync(log, cts.Token));
        Assert.Equal(EntityState.Detached, context.Entry(log).State);
    }

    [Fact]
    public async Task LogAsync_WhenPersistenceFails_InvokesOnAuditFailedCallback()
    {
        using var context = TestDbContext.Create();
        context.FailOnSaveChangesAsync = true;

        AuditFailureContext? capturedFailure = null;
        var options = new UserActivityLoggerOptions
        {
            OnAuditFailed = failureCtx => capturedFailure = failureCtx
        };

        var service = new UserActivityLoggerService<TestDbContext>(
            NullLogger<UserActivityLoggerService<TestDbContext>>.Instance,
            context,
            _httpContextAccessor,
            options);

        var log = new UserActivityLog
        {
            Event = "User transfer funds",
            Path = "/transfer",
            Method = "POST"
        };

        await service.LogAsync(log);

        Assert.NotNull(capturedFailure);
        Assert.Same(log, capturedFailure.Log);
        Assert.NotNull(capturedFailure.Exception);
    }

    [Fact]
    public async Task LogAsync_WhenOnAuditFailedCallbackThrows_DoesNotCrashCaller()
    {
        using var context = TestDbContext.Create();
        context.FailOnSaveChangesAsync = true;

        var options = new UserActivityLoggerOptions
        {
            FailureBehavior = AuditFailureBehavior.LogAndContinue,
            OnAuditFailed = _ => throw new InvalidOperationException("External alerting endpoint failed.")
        };

        var service = new UserActivityLoggerService<TestDbContext>(
            NullLogger<UserActivityLoggerService<TestDbContext>>.Instance,
            context,
            _httpContextAccessor,
            options);

        var log = new UserActivityLog
        {
            Event = "User login",
            Path = "/login",
            Method = "POST"
        };

        // Act - should not throw despite throwing callback
        await service.LogAsync(log);

        Assert.Equal(EntityState.Detached, context.Entry(log).State);
    }
}

