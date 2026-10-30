# Getting started

This guide gets `UserActivityLogger` installed and writing audit records in an ASP.NET Core MVC application.

**Package version:** `1.0.10`

**Target framework:** `net8.0`, `net9.0`, or `net10.0`
**Persistence:** Entity Framework Core

---

## Prerequisites

- An ASP.NET Core MVC application targeting `net8.0`, `net9.0`, or `net10.0`.
- Entity Framework Core configured with a database provider.
- A database where the audit table can be created.
- Permission to add and apply an EF Core migration.

---

## 1. Install the package

```bash
dotnet add package UserActivityLogger --version 1.0.10
```

Or add the reference directly:

```xml
<PackageReference Include="UserActivityLogger" Version="1.0.10" />
```

> The package requires license acceptance in its NuGet metadata. Some clients will prompt for confirmation.

---

## 2. Configure the `DbContext`

Add the audit entity configuration to your context:

```csharp
using Microsoft.EntityFrameworkCore;
using UserActivityLogger.Extensions;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureUserActivityLogger();
    }
}
```

`ConfigureUserActivityLogger()` registers `UserActivityLog` with its key, property lengths, and indexes.

### Optional `DbSet`

A `DbSet` is not required; the logger uses `Set<UserActivityLog>()` internally. If you want convenient querying, add one:

```csharp
using UserActivityLogger.Entities;

public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
```

> **Table-name note:** EF Core conventionally uses the `DbSet` property name for the table name. The sample migration in this repository creates `UserActivityLog`. If you add or rename a `DbSet`, review the generated migration, or pin the table name explicitly:
>
> ```csharp
> builder.Entity<UserActivityLog>().ToTable("UserActivityLog");
> ```

---

## 3. Register the library

Both service registration and MVC filter registration are required.

```csharp
using Microsoft.EntityFrameworkCore;
using UserActivityLogger.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddUserActivityLogger<AppDbContext>(options =>
{
    options.EnableRequestBodyLogging = false;
    options.LogAnonymousUsers = true;
    options.SkipPaths = new[] { "/health", "/metrics" };
});

builder.Services.AddControllersWithViews(options =>
{
    options.AddUserActivityLogging();
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

For an API-only application, use `AddControllers`:

```csharp
builder.Services.AddControllers(options =>
{
    options.AddUserActivityLogging();
});
```

### Why both calls?

- `AddUserActivityLogger<TContext>()` registers the options, `IHttpContextAccessor`, `IUserActivityLogger`, and the filter in DI.
- `AddUserActivityLogging()` adds that filter to the global MVC filter pipeline.

`AddUserActivityLogger<TContext>()` does **not** register `TContext`; your `AddDbContext<AppDbContext>()` call is still required.

### Minimal options

The defaults are usually enough to start:

```csharp
builder.Services.AddUserActivityLogger<AppDbContext>();
```

### Dedicated audit database or schema

If you want audit records persisted to a dedicated database or schema independently of the application's business `DbContext`, use `AddDedicatedUserActivityLogger`:

```csharp
builder.Services.AddDedicatedUserActivityLogger(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AuditConnection"));
});
```

This registers `IDbContextFactory<AuditDbContext>` and automatically configures `AuditDbContext`.

The default configuration:

| Option | Default |
|---|---|
| Form body logging | Disabled |
| Anonymous logging | Enabled |
| User claim | `ClaimTypes.NameIdentifier` |
| Skip paths | `/health`, `/metrics` |
| Redacted form fields | `password`, `pwd`, `secret`, `token`, `apikey`, `api_key`, `authorization` |

For all options, see [Security and Configuration](security-and-configuration.md).

---

## 4. Create and apply the migration

The package does **not** include an application migration. Generate one in your application.

### .NET CLI

```bash
dotnet ef migrations add AddUserActivityLogging \
  --project ./MyApp \
  --startup-project ./MyApp

dotnet ef database update \
  --project ./MyApp \
  --startup-project ./MyApp
```

### Package Manager Console

```powershell
Add-Migration AddUserActivityLogging
Update-Database
```

The migration creates the `UserActivityLog` table with:

| Column | SQL Server type | Notes |
|---|---|---|
| `Id` | `int` identity | Primary key. |
| `ResponseStatusCode` | `int` | HTTP status. |
| `Event` | `nvarchar(500)` | Required event description. |
| `IPAddress` | `nvarchar(50)` | Nullable. |
| `Path` | `nvarchar(2000)` | Nullable; includes the query string in the current version. |
| `Method` | `nvarchar(10)` | Nullable. |
| `AdditionalData` | `nvarchar(max)` | Nullable JSON payload. |
| `DateEvent` | `datetime2` | Timestamp. |
| `UserId` | `nvarchar(450)` | Nullable; no foreign key by default. |

Other EF Core providers map these types to their equivalents.

> Apply the migration before enabling logging. If the table is missing, the logger catches the database exception, writes it to `ILogger`, and the HTTP request still succeeds without an audit row.

---

## 5. Add your first loggable action

```csharp
using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Attributes;

public class HomeController : Controller
{
    [LogUserActivity("View dashboard")]
    public IActionResult Index()
    {
        return View();
    }
}
```

> **Important:** version `1.0.10` does not have a parameterless attribute constructor. `[LogUserActivity]` alone will not compile. Pass a description, or pass `null` to use the action's display name:
>
> ```csharp
> [LogUserActivity(null)]
> public IActionResult Index() => View();
> ```

---

## 6. Verify the setup

1. Apply the migration.
2. Run the application.
3. Request the decorated action.
4. Query the audit table:

```csharp
using Microsoft.EntityFrameworkCore;
using UserActivityLogger.Entities;

var latest = await db.Set<UserActivityLog>()
    .OrderByDescending(log => log.DateEvent)
    .FirstOrDefaultAsync();

Console.WriteLine(latest?.Event);
```

If no record appears, see [Troubleshooting](troubleshooting.md).

---

## Next steps

- [Usage Guide](usage-guide.md) — attributes, form logging, conditional descriptions, manual logging.
- [Architecture Overview](architecture-overview.md) — filter/service flow and data model.
- [Security and Configuration](security-and-configuration.md) — options, PII, redaction, IP handling.
- [Troubleshooting](troubleshooting.md) — common setup and runtime issues.
