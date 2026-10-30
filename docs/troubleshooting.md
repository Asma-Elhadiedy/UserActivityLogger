# Troubleshooting

This guide covers common problems with `UserActivityLogger` 1.0.10 and how to diagnose them.

---

## 1. First checks

If logs are missing, verify these in order:

1. **Package registered**
   ```csharp
   builder.Services.AddUserActivityLogger<AppDbContext>();
   ```

2. **MVC filter registered**
   ```csharp
   builder.Services.AddControllersWithViews(options =>
   {
       options.AddUserActivityLogging();
   });
   ```

3. **DbContext registered**
   ```csharp
   builder.Services.AddDbContext<AppDbContext>(options =>
       options.UseSqlServer(connectionString));
   ```

4. **Model configured**
   ```csharp
   protected override void OnModelCreating(ModelBuilder builder)
   {
       base.OnModelCreating(builder);
       builder.ConfigureUserActivityLogger();
   }
   ```

5. **Migration applied**
   Confirm that the `UserActivityLog` table exists in the target database.

6. **Action decorated**
   ```csharp
   [LogUserActivity("View dashboard")]
   ```

7. **Path not skipped**
   Default skips include `/health` and `/metrics`.

8. **User policy satisfied**
   If `LogAnonymousUsers` is `false`, an authenticated user with the configured claim is required.

9. **Check application logs**
   Search for:
   - `Failed to save user activity log`
   - `Failed to capture form data`
   - `Failed to serialize additional data`
   - `HttpContext is null, cannot log user activity`

---

## 2. Common issues

| Symptom | Likely cause | Fix |
|---|---|---|
| No audit rows at all | Filter not registered | Call `AddUserActivityLogging()` inside MVC registration. |
| No rows for one action | Attribute missing | Add `[LogUserActivity("...")]`; remember the description argument. |
| DI error at startup/request | Logger or context not registered | Register `AddUserActivityLogger<AppDbContext>()` and `AddDbContext<AppDbContext>()`. |
| `CS7036` compile error on the attribute | Parameterless attribute used | Pass a description or `null`. |
| `FormatException` | Description contains braces/placeholders | Use plain text or predefined messages. |
| `UserId` is null | No authenticated user or wrong claim | Configure authentication and `UserIdClaimType`. |
| Form data missing | Not a form request, or logging disabled | Enable `LogRequestBody` or `EnableRequestBodyLogging`; JSON is not captured. |
| Conditional description not applied | Flag/key missing | Set `LogActionArguments = true` and the exact `ConditionalLogAction` key. |
| IP is proxy/loopback | Reverse proxy or local request | Configure forwarded headers or a custom resolver. |
| Duplicate rows | Filter registered twice | Register `AddUserActivityLogging()` once. |
| Background job does not log | `LogActionAsync` has no HTTP context | Use `LogAsync` with an explicit entity. |
| `context.UserLogs` does not compile | No `DbSet` named `UserLogs` | Use `Set<UserActivityLog>()` or add a `DbSet`. |
| `Include(log => log.User)` does not compile | No navigation property | Configure your own relationship/navigation. |
| Action succeeds, then a 500 occurs | Logging dependency threw after result execution | Validate options and custom resolvers; inspect logs. |
| Rows disappear after a failed save | Database insert failed and was swallowed | Check `ILogger` and database constraints. |
| Table name changed after adding a `DbSet` | EF convention uses the property name | Pin with `ToTable(...)` and migrate. |

---

## 3. Missing filter registration

Symptom: no rows, even for decorated actions.

The service registration alone is not enough:

```csharp
// This registers the logger, but not the MVC filter.
builder.Services.AddUserActivityLogger<AppDbContext>();
```

Add the filter:

```csharp
builder.Services.AddControllersWithViews(options =>
{
    options.AddUserActivityLogging();
});
```

For API-only apps:

```csharp
builder.Services.AddControllers(options =>
{
    options.AddUserActivityLogging();
});
```

---

## 4. Missing logger or `DbContext` registration

The filter is global. MVC resolves it (and its dependencies) for requests even when the endpoint has no attribute. If the logger/context cannot be resolved, the request can fail with a DI exception.

Register both:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddUserActivityLogger<AppDbContext>();
```

`AddUserActivityLogger<TContext>()` does not register `TContext`.

---

## 5. `FormatException` on an action description

The filter calls:

```csharp
string.Format(logMessage, actionName, controllerName);
```

A custom description containing braces or unsupported placeholders can throw before the action runs.

Problem examples:

```csharp
[LogUserActivity("Export {format:csv}")]
[LogUserActivity("Payload {\"id\":1}")]
```

Avoid literal braces in custom descriptions. Use the predefined templates or plain text:

```csharp
[LogUserActivity("Export CSV report")]
```

If braces are required, fix/extend the library's formatting behavior rather than storing malformed templates.

---

## 6. User ID is null

Check the authentication pipeline. The following assumes an authentication scheme has already been registered (for example, cookies or JWT):

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Check the claim:

```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

Check the configured claim type:

```csharp
options.UserIdClaimType = ClaimTypes.NameIdentifier;
```

If the identity provider uses a different claim (for example, `sub`), configure it explicitly.

If you only want authenticated activity, set:

```csharp
options.LogAnonymousUsers = false;
```

With anonymous logging disabled, requests with no user ID are skipped.

---

## 7. Form data is not captured

`LogRequestBody` captures only form content.

Checklist:

- Is the request content type `application/x-www-form-urlencoded` or `multipart/form-data`?
- Is `LogRequestBody = true` on the action, or `EnableRequestBodyLogging = true` globally?
- Does the form contain fields?
- Are the fields skipped because their keys begin with `__`?
- Are they DataTables parameters (`columns[...]`, `order[...]`, `search[...]`, `draw`, `start`, `length`)?
- Are you sending JSON instead of a form?

JSON request bodies are not supported by the built-in capture path.

Form capture errors are logged as `Failed to capture form data`; the request continues with `AdditionalData` null.

---

## 8. Conditional description is ignored

Requirements:

```csharp
[LogUserActivity("Change status", LogActionArguments = true)]
public IActionResult ChangeStatus(int id)
{
    HttpContext.Items["ConditionalLogAction"] = "Activate department";
    return Ok();
}
```

- `LogActionArguments` must be `true`.
- The key must be exactly `"ConditionalLogAction"`.
- The value must be non-null and non-empty.
- Set the item before returning from the action.

The override is read after the result executes. It is not formatted with `string.Format`.

---

## 9. IP address is `127.0.0.1`, `::1`, or a proxy address

- Local requests commonly appear as loopback. `::1` is normalized to `127.0.0.1`.
- Behind a proxy, `Connection.RemoteIpAddress` is usually the proxy address.
- Configure forwarded headers with a trusted-proxy policy:

```csharp
using Microsoft.AspNetCore.HttpOverrides;

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

app.UseForwardedHeaders();
```

Do not trust arbitrary `X-Forwarded-For` values from clients. If using a custom `IpAddressResolver`, keep the return value at 50 characters or fewer and handle null/empty values.

---

## 10. Duplicate rows for one request

Likely causes:

- the filter added by more than one registration path,
- both controller-level and action-level attributes present,
- application code manually logging in addition to the attribute.

Register the filter:

```csharp
builder.Services.AddControllersWithViews(options =>
{
    options.AddUserActivityLogging();
});
```

Avoid combining automatic attribute logging with an equivalent manual `LogActionAsync` call in the same action unless duplicate records are intentional.

Calling `AddUserActivityLogging()` multiple times is safe.
The filter is only registered once.

Repeated calls to `AddUserActivityLogger<TContext>()` without
new configuration preserve the original registrations.

Attempting to configure the logger again throws an
`InvalidOperationException`.

---

## 11. Background jobs do not create records

`LogActionAsync` requires a current HTTP request:

```csharp
// In a background job this logs a warning and returns.
await _activityLogger.LogActionAsync("Generate report");
```

Use `LogAsync` instead:

```csharp
await _activityLogger.LogAsync(new UserActivityLog
{
    Event = "Generate report",
    UserId = systemUserId,
    Path = "/jobs/reports",
    Method = "BACKGROUND",
    IPAddress = "N/A",
    ResponseStatusCode = 200,
    DateEvent = DateTime.UtcNow
});
```

`LogAsync` still uses the scoped `DbContext`; ensure the job has a valid scope and a reachable database.

---

## 12. No rows, but no visible exception

The service catches database exceptions and logs them, then lets the request continue. Check `ILogger` output for `Failed to save user activity log`.

Common causes:

- the migration was not applied,
- the connection string points to another database,
- the table/schema name does not match the model,
- a column length was exceeded (`Event` 500, `Path` 2000, `IPAddress` 50, `Method` 10, `UserId` 450),
- the database is unavailable,
- `SkipPaths` matched the request,
- anonymous logging is disabled and the user has no claim.

The audit write is not transactional with the business operation; a failed write does not fail the HTTP request.

---

## 13. `context.UserLogs` or `Include(log => log.User)` does not compile

The entity is `UserActivityLog`, and the sample context does not declare a `DbSet` property.

Use:

```csharp
var logs = await context.Set<UserActivityLog>().ToListAsync();
```

Or add your own property:

```csharp
public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
```

There is no `User` navigation property by default. Configure a relationship and navigation if you need `Include`.

---

## 14. Action succeeds, then a 500 is returned

The logger runs after result execution. Errors in logging can surface after the action has completed:

- a custom `IpAddressResolver` throws,
- `SkipPaths` or `RedactedFields` is null,
- an options consumer misconfigures a dependency,
- an unexpected error occurs while building the audit record.

Validate options at startup and make custom resolvers defensive:

```csharp
options.SkipPaths ??= Array.Empty<string>();
options.RedactedFields ??= Array.Empty<string>();

options.IpAddressResolver = context =>
{
    try
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
    catch
    {
        return "Unknown";
    }
};
```

---

## 15. Database exceptions are swallowed

`LogAsync` is intentionally fail-soft:

```csharp
try
{
    _context.Set<UserActivityLog>().Add(log);
    await _context.SaveChangesAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to save user activity log");
}
```

This keeps the HTTP request successful but means audit records can be lost silently unless you monitor application logs. If audit completeness is critical, add metrics/alerts, a retry/outbox pipeline, or a dedicated audit context.

---

## 16. Timestamps look wrong

`UserActivityLog.DateEvent` defaults to `DateTime.Now` (local time). The filter-created rows use that default.

For UTC records, set the value explicitly with `LogAsync`:

```csharp
await _activityLogger.LogAsync(new UserActivityLog
{
    Event = "Background job completed",
    DateEvent = DateTime.UtcNow
});
```

Filter-created records require extending the library or changing the default if UTC is mandatory.

---

## 17. JSON request bodies are not logged

This is expected. The current implementation only captures form content. If JSON auditing is required, implement a custom capture layer or extend `UserActivityLoggingFilter`.

---

## 18. Unhandled exceptions are not logged

The filter logs after `next()` completes. If the action/result throws, the log call is not reached. Authorization/resource short-circuits may also bypass the result filter.

If failed operations must be audited, add explicit logging in exception handling/middleware or a separate audit mechanism.

---

## FAQ

### Does it support minimal APIs?

No. The filter is registered through MVC options. Minimal API handlers can call `IUserActivityLogger.LogActionAsync` manually while an HTTP context exists.

### Does it work with PostgreSQL, MySQL, or SQLite?

Yes, as long as your `DbContext` uses an EF Core provider. The library does not reference a provider. Generate migrations with your provider.

### Can I use it without an attribute?

Yes. Inject `IUserActivityLogger` and call `LogActionAsync` or `LogAsync` manually.

### Can I log a custom user?

`LogAsync` accepts an explicit `UserActivityLog`, so set `UserId` yourself. `LogActionAsync` uses the configured claim from the current principal.

### Can I change the table name?

Yes:

```csharp
builder.Entity<UserActivityLog>().ToTable("UserActivityLog", schema: "audit");
```

Generate a migration after the change.

### Can I disable logging for an endpoint?

Do not apply the attribute, or add the path to `SkipPaths`.

### Does redaction handle query strings?

No. Query strings are stored in `Path` without redaction. See [Security and Configuration](security-and-configuration.md#5-query-string-handling).

### Does redaction handle `AdditionalData`?

No. Sanitize any custom data before passing it to the logger.

### Does the library retry failed writes?

No. Failures are logged and swallowed.

### Does it run logging in the background?

No. The audit insert executes inline with the request. There is no queue or background processor.

### Is the audit table tamper-proof?

No. It is a normal mutable EF Core table.

### Is `DateTime` UTC?

No. The entity default is `DateTime.Now` (local time).

---

## Related documentation

- [Getting Started](getting-started.md)
- [Usage Guide](usage-guide.md)
- [Architecture Overview](architecture-overview.md)
- [Security and Configuration](security-and-configuration.md)
