# UserActivityLogger

[![NuGet](https://img.shields.io/badge/NuGet-1.0.10-blue)](https://www.nuget.org/packages/UserActivityLogger)
[![CI](https://github.com/Asma-Elhadiedy/UserActivityLogger/actions/workflows/ci.yml/badge.svg)](https://github.com/Asma-Elhadiedy/UserActivityLogger/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A lightweight, attribute-driven user activity and audit logging library for ASP.NET Core MVC applications.

`UserActivityLogger` records who did what, when, from where, and with what result—without adding repetitive audit code to every controller action.

```csharp
[LogUserActivity("View dashboard")]
public IActionResult Index() => View();
```

---

## Why UserActivityLogger?

- **Low ceremony** — decorate an action instead of writing audit inserts.
- **Consistent context** — captures user, path, method, IP, status, and timestamp.
- **Opt-in by design** — only actions marked with `[LogUserActivity]` are logged.
- **Form redaction** — configurable redaction of sensitive form fields.
- **Conditional descriptions** — choose the event text based on the operation outcome.
- **EF Core storage** — audit rows live in your application's database.
- **Manual fallback** — use `IUserActivityLogger` outside the attribute path.

---

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
Application DbContext → UserActivityLog table
```

The filter writes the audit record after the action result executes. It captures the current HTTP context through `IHttpContextAccessor` and persists the entity through EF Core.

For a detailed walkthrough, see [Architecture Overview](docs/architecture-overview.md).

---

## Quick start

### 1. Install

```bash
dotnet add package UserActivityLogger --version 1.0.10
```

Requires an ASP.NET Core MVC application targeting `.NET 9` with EF Core configured.

### 2. Configure the `DbContext`

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

### 3. Register services and the MVC filter

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

Both registrations are required: `AddUserActivityLogger<TContext>()` registers the logger and its dependencies; `AddUserActivityLogging()` adds the global filter.

### 4. Apply the migration

The package does not ship an application migration. Create one in your application:

```bash
dotnet ef migrations add AddUserActivityLogging \
  --project ./MyApp \
  --startup-project ./MyApp

dotnet ef database update \
  --project ./MyApp \
  --startup-project ./MyApp
```

Package Manager Console:

```powershell
Add-Migration AddUserActivityLogging
Update-Database
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

> **Important:** version `1.0.10` has no parameterless attribute constructor. `[LogUserActivity]` alone will not compile. Use `[LogUserActivity("Description")]`, or `[LogUserActivity(null)]` to fall back to the action display name.

---

## Documentation

For a comprehensive overview of all documentation, visit the [Documentation Index](docs/README.md).

| Guide | Contents |
|---|---|
| [Documentation Index](docs/README.md) | Central table of contents for guides, architecture, and policies. |
| [Getting Started](docs/getting-started.md) | Installation, `DbContext`, `Program.cs`, migrations, verification. |
| [Usage Guide](docs/usage-guide.md) | Attributes, predefined messages, form logging, conditional overrides, manual logging. |
| [Architecture Overview](docs/architecture-overview.md) | Filter/service flow, entity model, indexes, limitations. |
| [Architecture Decisions (ADRs)](docs/decisions/README.md) | Historical record of design and architectural decisions. |
| [Security and Configuration](docs/security-and-configuration.md) | Options, PII, redaction, query strings, proxy IP handling. |
| [Troubleshooting](docs/troubleshooting.md) | Common setup and runtime issues, FAQs. |

---

## Important behavior and limitations

- **Form bodies only.** Request-body capture supports form-encoded requests, not JSON.
- **Query strings are stored in `Path`.** The current implementation decodes and stores query strings without redaction; do not put secrets in them.
- **Redaction is form-key based.** `AdditionalData`, query strings, and values under unrecognized keys are not redacted automatically.
- **Result filter.** Exceptions that bypass result execution are generally not logged; authorization/resource short-circuits may not reach the filter.
- **Shared `DbContext`.** Audit writes call `SaveChangesAsync()` on the scoped application context, so pending business changes can be committed with the log.
- **Best-effort persistence.** Database write failures are logged to `ILogger` and swallowed; the HTTP request still succeeds.
- **Local timestamps.** `DateEvent` defaults to `DateTime.Now`; set it explicitly with `LogAsync` if UTC is required.
- **No background queue.** Writes happen inline with the request. There is no retry, batching, or outbox.

For details and mitigations, see [Security and Configuration](docs/security-and-configuration.md) and [Architecture Overview](docs/architecture-overview.md).

---

## Compatibility

| Item | Value |
|---|---|
| Target framework | `net8.0`, `net9.0`, `net10.0` |
| UI integration | ASP.NET Core MVC |
| Persistence | Entity Framework Core 8, 9, 10 |
| Database providers | Any EF Core provider |
| Current package version | `1.0.10` |

Minimal APIs, automatic JSON body capture, and built-in retention/archival are not supported by this version.

---

## Community & Contributing

- [Contributing Guidelines](CONTRIBUTING.md) — Local development setup, coding standards, and PR process.
- [Code of Conduct](CODE_OF_CONDUCT.md) — Community pledge and guidelines.
- [Security Policy](SECURITY.md) — Vulnerability reporting instructions and scope.
- [Support Policy](SUPPORT.md) — Getting help through documentation and discussions.
- [Changelog](CHANGELOG.md) — Release history and version updates.

## License

MIT. See [LICENSE](LICENSE).
