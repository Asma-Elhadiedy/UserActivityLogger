
namespace UserActivityLogger.Extensions;

public static class ModelBuilderExtensions
{
    public static void ConfigureUserActivityLogger(this ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Event).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.IPAddress).HasMaxLength(50);
            entity.Property(e => e.Path).HasMaxLength(2000);
            entity.Property(e => e.Method).HasMaxLength(10);

            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.DateEvent);
            entity.HasIndex(e => e.Event);
            entity.HasIndex(e => new { e.UserId, e.DateEvent });
        });
    }
}