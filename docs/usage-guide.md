# Usage guide

This guide covers the public usage patterns for `UserActivityLogger` 1.0.10.

For setup, see [Getting Started](getting-started.md). For security and option details, see [Security and Configuration](security-and-configuration.md).

---

## 1. Log a controller action

Add `[LogUserActivity]` to the action you want to audit:

```csharp
using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Attributes;

[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController : ControllerBase
{
    [HttpGet]
    [LogUserActivity("View dashboard")]
    public IActionResult Get()
    {
        return Ok();
    }
}
```

The filter writes the event after the action result executes.

### Important: pass a description

Version `1.0.10` has no parameterless attribute constructor. This does **not** compile:

```csharp
[LogUserActivity] // CS7036: missing actionDescription argument
public IActionResult Index() => View();
```

Use one of these forms:

```csharp
[LogUserActivity("View dashboard")]
public IActionResult Index() => View();

[LogUserActivity(null)]
public IActionResult Index() => View();
```

Passing `null` makes the filter fall back to `ActionDescriptor.DisplayName`, which is usually less readable than an explicit description.

### Controller-level attribute

The attribute can also be applied to a controller class:

```csharp
using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Attributes;

[LogUserActivity("Manage departments")]
public sealed class DepartmentsController : Controller
{
    public IActionResult Index() => View();
}
```

Prefer action-level attributes when different actions need different event descriptions. Avoid placing attributes on both the controller and action unless you have verified which metadata entry the filter selects.

---

## 2. Attribute parameters

```csharp
using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Attributes;

public sealed class ProfileController : Controller
{
    [HttpPost]
    [LogUserActivity(
        "Update profile",
        LogRequestBody = true,
        LogActionArguments = false)]
    public IActionResult UpdateProfile([FromForm] IFormCollection form)
    {
        return Ok();
    }
}
```

| Property | Type | Default | Purpose |
|---|---|---|---|
| `ActionDescription` | `string?` | Constructor value | Event text. Supports the predefined `{0}`/`{1}` placeholders. |
| `LogRequestBody` | `bool` | `false` | Enables form-field capture for this action. |
| `LogActionArguments` | `bool` | `false` | Enables the conditional description override. It does **not** log action arguments. |

### Description formatting caution

The filter applies `string.Format` to the description with two arguments: the route action name and controller name.

- `{0}` is replaced with the action name.
- `{1}` is replaced with the controller name.
- Literal braces in custom text can throw `FormatException`.
- Placeholders such as `{2}` or format specifiers can throw.

Use plain text for custom descriptions:

```csharp
[LogUserActivity("Export department report")]
```

Do not use JSON-like text in the description:

```csharp
// Avoid: braces are interpreted as format syntax.
[LogUserActivity("Payload {\"id\":1}")]
```

---

## 3. Predefined messages

The package provides message templates in `UserActivityLogger.Constants`:

```csharp
using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Attributes;
using UserActivityLogger.Constants;

public sealed class DepartmentsController : Controller
{
    [HttpGet]
    [LogUserActivity(ConstPredefinedLogMessage.UserVisitedPage)]
    public IActionResult Index() => View();

    [HttpPost]
    [LogUserActivity(ConstPredefinedLogMessage.UserCreatedResource)]
    public IActionResult Create() => Ok();

    [HttpPost]
    [LogUserActivity(ConstPredefinedLogMessage.UserUpdatedResource)]
    public IActionResult Edit() => Ok();

    [HttpPost]
    [LogUserActivity(ConstPredefinedLogMessage.UserDeletedResource)]
    public IActionResult Delete() => Ok();
}
```

Templates:

| Constant | Template | Example |
|---|---|---|
| `UserVisitedPage` | `User Visited {0} page in {1}.` | `User Visited Index page in Departments.` |
| `UserCreatedResource` | `User created a resource of type {1}.` | `User created a resource of type Departments.` |
| `UserUpdatedResource` | `User updated a resource of type {1}.` | `User updated a resource of type Departments.` |
| `UserDeletedResource` | `User deleted a resource of type {1}.` | `User deleted a resource of type Departments.` |

`{0}` is the action name and `{1}` is the controller name. The create/update/delete templates intentionally use only `{1}`.

For application-specific events, define your own constants rather than scattering strings:

```csharp
public static class ActivityEvents
{
    public const string ExportPayroll = "Export payroll";
    public const string ApproveInvoice = "Approve invoice";
}
```

---

## 4. Form body logging and redaction

Enable form capture per action:

```csharp
using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Attributes;

public sealed class ProfileController : Controller
{
    [HttpPost]
    [LogUserActivity("Update profile", LogRequestBody = true)]
    public IActionResult UpdateProfile([FromForm] ProfileInputModel model)
    {
        return Ok();
    }
}

public sealed class ProfileInputModel
{
    public string? DisplayName { get; set; }
    public string? Password { get; set; }
}
```

Or enable it globally:

```csharp
builder.Services.AddUserActivityLogger<AppDbContext>(options =>
{
    options.EnableRequestBodyLogging = true;
});
```

The two switches are OR'ed: setting `LogRequestBody = false` on a single action does not disable global form capture.

### What is captured

- Only `application/x-www-form-urlencoded` and `multipart/form-data` requests.
- Form fields are stored as a JSON object in `AdditionalData`.
- Multi-value fields are flattened to a single string.
- Boolean strings are normalized to `True`/`False`.
- Uploaded files are not captured.
- JSON request bodies are not captured by this feature.

### Redaction

The default redacted form keys are:

```text
password, pwd, secret, token, apikey, api_key, authorization
```

Matching is case-insensitive and substring-based. For example, `newPassword`, `passwordHash`, and `api_key` all match.

Configure the list at startup:

```csharp
builder.Services.AddUserActivityLogger<AppDbContext>(options =>
{
    options.RedactedFields = new[]
    {
        "password", "pwd", "secret", "token",
        "apikey", "api_key", "authorization",
        "creditcard", "cardnumber", "cvv", "ssn", "pin"
    };

    options.RedactionPlaceholder = "***REDACTED***";
});
```

Redaction applies only to **form keys captured by the filter**. It does not apply to:

- query strings,
- `AdditionalData` supplied to `LogActionAsync` or `LogAsync`,
- values whose key does not contain a configured term,
- headers.

Never use redaction as a substitute for keeping secrets out of logs in the first place.

### Skipped form keys

The filter skips:

- keys beginning with `__` (including anti-forgery tokens),
- DataTables parameters such as `columns[...]`, `order[...]`, `search[...]`, `draw`, `start`, and `length`.

---

## 5. Conditional descriptions

Use `LogActionArguments = true` when an action can produce different audit events:

```csharp
using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Attributes;

public interface IDepartmentStatusService
{
    bool ToggleStatus(int id);
}

public sealed class DepartmentsController : Controller
{
    private readonly IDepartmentStatusService _statusService;

    public DepartmentsController(IDepartmentStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpPost]
    [LogUserActivity("Change department status", LogActionArguments = true)]
    public IActionResult ChangeStatus(int id)
    {
        var activated = _statusService.ToggleStatus(id);

        HttpContext.Items["ConditionalLogAction"] = activated
            ? "Activate department"
            : "Deactivate department";

        return Ok();
    }
}
```

Rules:

- `LogActionArguments` must be `true`.
- The key must be exactly `"ConditionalLogAction"`.
- The value must be non-null and non-empty.
- Set the item before returning from the action; the filter reads it after result execution.
- The override is stored as-is and is not passed through `string.Format`.

A local constant avoids typos:

```csharp
public static class ActivityLogKeys
{
    public const string ConditionalAction = "ConditionalLogAction";
}
```

```csharp
HttpContext.Items[ActivityLogKeys.ConditionalAction] = "Activate department";
```

If more than one attribute is discoverable on the endpoint, the filter uses the first one. Prefer a single action-level attribute.

---

## 6. Manual logging

Inject `IUserActivityLogger` when logging from code that is not covered by the attribute.

### `LogActionAsync` — current HTTP request

Use `LogActionAsync` inside a controller, middleware, or service that is running as part of an HTTP request:

```csharp
using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Services;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IUserActivityLogger _activityLogger;

    public ReportsController(IUserActivityLogger activityLogger)
    {
        _activityLogger = activityLogger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate()
    {
        // Business logic goes here.

        await _activityLogger.LogActionAsync(
            "Generate report",
            new { ReportType = "Q4", GeneratedAt = DateTime.UtcNow });

        return Ok();
    }
}
```

`LogActionAsync`:

- reads the current `HttpContext` through `IHttpContextAccessor`,
- checks `SkipPaths`,
- resolves the user ID and applies `LogAnonymousUsers`,
- captures path, method, IP, and response status,
- serializes `additionalData` to JSON,
- calls `LogAsync`.

If there is no HTTP context (for example, a background job), it logs a warning and returns without writing a record.

### `LogAsync` — explicit record

Use `LogAsync` for background jobs, scheduled tasks, or when you need full control over the entity:

```csharp
using System.Text.Json;
using UserActivityLogger.Entities;
using UserActivityLogger.Services;

public sealed class ReportJob
{
    private readonly IUserActivityLogger _activityLogger;

    public ReportJob(IUserActivityLogger activityLogger)
    {
        _activityLogger = activityLogger;
    }

    public async Task RunAsync(string systemUserId)
    {
        // Generate the report.

        await _activityLogger.LogAsync(new UserActivityLog
        {
            Event = "Generate report",
            UserId = systemUserId,
            Path = "/jobs/reports",
            Method = "BACKGROUND",
            IPAddress = "N/A",
            ResponseStatusCode = 200,
            AdditionalData = JsonSerializer.Serialize(new
            {
                ReportType = "Q4",
                GeneratedAt = DateTime.UtcNow
            }),
            DateEvent = DateTime.UtcNow
        });
    }
}
```

`LogAsync`:

- does not require an HTTP context,
- accepts a fully constructed `UserActivityLog`,
- uses the scoped `TContext` and calls `SaveChangesAsync()`,
- catches and logs persistence exceptions without informing the caller.

### When to use which

| Method | Use when | Requires `HttpContext` |
|---|---|---|
| `LogActionAsync` | Code runs within an HTTP request and should inherit request context. | Yes |
| `LogAsync` | Background work or fully custom records. | No |

> **Transaction note:** by default (`IsolatePersistence = true`), audit persistence executes on an isolated `DbContext` instance and does not flush pending business changes from the request's scoped `DbContext`. When configured with `IsolatePersistence = false`, the logger writes through the scoped context and flushes pending changes. See [Architecture Overview](architecture-overview.md#74-dbcontext-isolation-and-unit-of-work).

---

## 7. Querying audit records

With a `DbSet<UserActivityLog> UserActivityLogs` property:

```csharp
var recent = await db.UserActivityLogs
    .Where(log => log.UserId == currentUserId)
    .OrderByDescending(log => log.DateEvent)
    .Take(50)
    .ToListAsync();
```

Without a `DbSet`:

```csharp
var recent = await db.Set<UserActivityLog>()
    .Where(log => log.UserId == currentUserId)
    .OrderByDescending(log => log.DateEvent)
    .Take(50)
    .ToListAsync();
```

The entity has no user navigation property. `Include(log => log.User)` requires your own relationship/navigation configuration.

---

## Quick reference

```csharp
// Basic action
[LogUserActivity("View dashboard")]

// Fallback to ActionDescriptor.DisplayName
[LogUserActivity(null)]

// Form logging
[LogUserActivity("Update profile", LogRequestBody = true)]

// Conditional description
[LogUserActivity("Change status", LogActionArguments = true)]
HttpContext.Items["ConditionalLogAction"] = "Activate department";
```
