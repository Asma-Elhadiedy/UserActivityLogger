# UserActivityLogger

[![NuGet](https://img.shields.io/badge/NuGet-1.0.10-blue)](https://www.nuget.org/packages/UserActivityLogger)
[![CI](https://github.com/Asma-Elhadiedy/UserActivityLogger/actions/workflows/ci.yml/badge.svg)](https://github.com/Asma-Elhadiedy/UserActivityLogger/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

An audit logging library for ASP.NET Core MVC applications.

UserActivityLogger records user identity, route path, HTTP method, client IP address, response status code, and timestamp for actions decorated with the `[LogUserActivity]` attribute.

```csharp
[LogUserActivity("View dashboard")]
public IActionResult Index() => View();
```

## Features

- Attribute decoration: Opt-in audit logging via `[LogUserActivity]`.
- Request context: Captures user ID, path, query string, HTTP method, client IP, response status, and timestamp.
- Form data redaction: Configurable redaction for sensitive form fields.
- Conditional event messages: Override event descriptions based on execution outcome.
- Entity Framework Core storage: Writes audit entities directly through your application DbContext.
- Manual interface: Direct logging via `IUserActivityLogger` outside the attribute filter pipeline.

## How it works

```text
Controller action
      ↓
[LogUserActivity] metadata
      ↓
UserActivityLoggingFilter (global MVC result filter)
      ↓
UserActivityLoggerService<TContext>
      ↓
Application DbContext -> UserActivityLog table
```

The filter writes the audit record after the action result executes. It reads request context through `IHttpContextAccessor` and persists the entity through Entity Framework Core.

For architectural details, see the [Architecture overview](docs/architecture-overview.md).

## Quick start

### 1. Install package

```bash
dotnet add package UserActivityLogger --version 1.0.10
```

Requires an ASP.NET Core MVC application targeting `net8.0`, `net9.0`, or `net10.0` with Entity Framework Core configured.

### 2. Configure DbContext

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

### 3. Register services and filter

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

Both calls are required: `AddUserActivityLogger<TContext>()` registers the logger and options in dependency injection; `AddUserActivityLogging()` adds the result filter to MVC options.

### 4. Apply migration

The package does not contain migrations. Add and apply a migration in the consuming project:

```bash
dotnet ef migrations add AddUserActivityLogging
dotnet ef database update
```

### 5. Decorate an action

```csharp
using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Attributes;

public sealed class HomeController : Controller
{
    [LogUserActivity("View dashboard")]
    public IActionResult Index()
    {
        return View();
    }
}
```

Note: In version 1.0.10, `LogUserActivityAttribute` requires an action description argument. Use `[LogUserActivity("Description")]` or `[LogUserActivity(null)]` to use the action display name.

## Documentation

For a directory of all guides, see the [Documentation index](docs/README.md).

- [Getting started](docs/getting-started.md): Installation, DbContext setup, and migrations.
- [Usage guide](docs/usage-guide.md): Attributes, predefined messages, form logging, and manual logging.
- [Architecture overview](docs/architecture-overview.md): Pipeline flow, entity schema, and indexes.
- [Architecture decision records](docs/decisions/README.md): Record of design and technical decisions.
- [Security and configuration](docs/security-and-configuration.md): Options, redaction, and proxy handling.
- [Troubleshooting](docs/troubleshooting.md): Common setup issues and diagnostic checks.

## Important behavior and limitations

- Form bodies only: Request body capture reads form-encoded content (`Request.HasFormContentType`). JSON payloads are not captured.
- Query strings in path: Query strings are appended to `Path` without redaction. Do not pass credentials or secrets in query parameters.
- Form-key redaction: Redaction applies only to form field names matching configured terms. Values in `AdditionalData` or query strings are not redacted.
- Result filter boundary: The filter executes after result execution. Unhandled exceptions that bypass result execution are not logged by the filter.
- Shared DbContext unit of work: Persistence calls `SaveChangesAsync()` on the scoped application context. Pending entity changes in the same scope are saved together.
- Inline persistence: Writes occur synchronously within the HTTP request pipeline. There is no background queue or retry mechanism.
- Best-effort persistence: Database write exceptions are caught, logged via `ILogger`, and suppressed. The client response still succeeds.
- Local timestamps: `DateEvent` defaults to `DateTime.Now` (server local time). Set `DateEvent` explicitly via `LogAsync` if UTC is required.

## Compatibility

| Item | Value |
|---|---|
| Target framework | `net8.0`, `net9.0`, `net10.0` |
| UI integration | ASP.NET Core MVC |
| Persistence | Entity Framework Core 8, 9, 10 |
| Database providers | Any Entity Framework Core provider |
| Current package version | `1.0.10` |

Minimal APIs, automated JSON body capture, and built-in retention jobs are not supported in version 1.0.10.

## Community and contributing

- [Contributing guidelines](CONTRIBUTING.md): Environment setup, coding conventions, and pull request process.
- [Code of conduct](CODE_OF_CONDUCT.md): Participation standards and reporting.
- [Security policy](SECURITY.md): Vulnerability reporting instructions and scope.
- [Support policy](SUPPORT.md): Questions, issue triage, and discussion channels.
- [Changelog](CHANGELOG.md): Historical record of releases and changes.

## License

MIT. See [LICENSE](LICENSE).
