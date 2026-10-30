using Microsoft.EntityFrameworkCore;
using UserActivityLogger.Extensions;

namespace UserActivityLogger.Entities;

/// <summary>
/// A dedicated <see cref="DbContext"/> for persisting user activity logs independently of application business contexts.
/// </summary>
public class AuditDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditDbContext"/> class for testing or inheritance.
    /// </summary>
    protected AuditDbContext()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the <see cref="DbContext"/>.</param>
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for user activity logs.
    /// </summary>
    public virtual DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureUserActivityLogger();
    }
}
