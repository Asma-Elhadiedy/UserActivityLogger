# Security and Configuration

This guide documents the configuration surface of `UserActivityLogger` 1.0.10 and the security considerations that matter when audit records contain user, request, and form data.

---

## 1. Configuration model

Options are configured once during service registration:

```csharp
using UserActivityLogger.Extensions;

builder.Services.AddUserActivityLogger<AppDbContext>(options =>
{
    options.EnableRequestBodyLogging = false;
    options.LogAnonymousUsers = false;
    options.SkipPaths = new[] { "/health", "/metrics" };
});
```

The options object is created at startup and registered as a singleton. Changes require an application restart; there is no `IOptionsMonitor` reload behavior. The library does not validate options, so null arrays and invalid values can fail at request time.

---

## 2. Options reference

### `UserActivityLoggerOptions`

| Property | Type | Default | Behavior |
|---|---|---|---|
| `EnableRequestBodyLogging` | `bool` | `false` | Enables form-field capture for all decorated actions. OR'ed with the per-action `LogRequestBody` value. |
| `RedactedFields` | `string[]` | `password`, `pwd`, `secret`, `token`, `apikey`, `api_key`, `authorization` | Case-insensitive, substring-based matches against form keys. |
| `SkipPaths` | `string[]` | `/health`, `/metrics` | Segment-aware, case-insensitive path prefixes checked by the service. |
| `UserIdClaimType` | `string` | `ClaimTypes.NameIdentifier` | Claim used for `UserActivityLog.UserId`. |
| `LogAnonymousUsers` | `bool` | `true` | When `false`, requests without a user ID are skipped. |
| `IpAddressResolver` | `Func<HttpContext, string>?` | `null` | Custom resolver; falls back to the remote IP when it returns null/empty. |
| `RedactionPlaceholder` | `string` | `***REDACTED***` | Replacement value for redacted form fields. |

### Per-action attribute options

| Property | Type | Default | Behavior |
|---|---|---|---|
| `ActionDescription` | `string?` | Constructor value | Event text. Formatted with `{0}` = action, `{1}` = controller. |
| `LogRequestBody` | `bool` | `false` | Enables form capture for the action. |
| `LogActionArguments` | `bool` | `false` | Enables the `ConditionalLogAction` override. Does **not** log action arguments. |

### Example configuration

```csharp
builder.Services.AddUserActivityLogger<AppDbContext>(options =>
{
    options.EnableRequestBodyLogging = false;
    options.LogAnonymousUsers = false;
    options.UserIdClaimType = ClaimTypes.NameIdentifier;

    options.RedactedFields = new[]
    {
        "password", "pwd", "secret", "token",
        "apikey", "api_key", "authorization",
        "creditcard", "cardnumber", "cvv", "ssn", "pin"
    };

    options.RedactionPlaceholder = "***REDACTED***";
    options.SkipPaths = new[] { "/health", "/metrics", "/swagger" };
});
```

### `SkipPaths` semantics

`SkipPaths` uses `PathString.StartsWithSegments` with ordinal-ignore-case comparison:

- `/health` matches `/health` and `/health/live`.
- `/health` does **not** match `/healthz`.
- Matching is based on path segments, not raw string prefixes.

Skipping happens inside `LogActionAsync`. The global filter may still parse form data and resolve the description before the service decides to skip.

### Defaults and null safety

Initialize arrays if they may be assigned from configuration:

```csharp
options.RedactedFields ??= Array.Empty<string>();
options.SkipPaths ??= Array.Empty<string>();
```

A null `SkipPaths` can cause a request-time exception. A null `RedactedFields` causes form capture to fail (the error is logged and the body is not stored).

---

## 3. PII and data minimization

Audit records can contain:

- user identifiers,
- IP addresses,
- request paths and query strings,
- form values,
- arbitrary JSON in `AdditionalData`.

Treat the audit table as sensitive data:

- Store only what the audit purpose requires.
- Define and enforce a retention period.
- Restrict database and application access.
- Avoid logging full forms for high-risk operations.
- Document deletion/anonymization procedures.
- Consider whether IP addresses and user IDs must be hashed or encrypted for your jurisdiction.

The library does not provide retention, anonymization, encryption, or access-control features.

---

## 4. Form redaction

### Defaults

```text
password, pwd, secret, token, apikey, api_key, authorization
```

Matching is:

- case-insensitive,
- substring-based,
- applied to form **keys**, not values.

Examples that match:

- `password`
- `newPassword`
- `passwordHash`
- `api_key`
- `Authorization`

### What redaction does not cover

- Query-string values.
- `AdditionalData` passed to `LogActionAsync` or `LogAsync`.
- Form values under keys that do not contain a configured term.
- Headers.
- JSON request bodies (not captured by this library).

Do not use redaction as the only protection for sensitive data. Keep secrets out of loggable fields whenever possible.

### Custom fields

```csharp
options.RedactedFields = new[]
{
    "password", "pwd", "secret", "token",
    "apikey", "api_key", "authorization",
    "creditcard", "cardnumber", "cvv", "ssn", "pin"
};
```

Review the list against your domain. Overly broad terms can hide useful audit data; overly narrow terms can leak sensitive values.

---

## 5. Query-string handling

> **Security warning:** the current implementation stores the full request path **including the decoded query string**, and query parameters are not redacted.

For example:

```text
/reset?token=abc123
```

is persisted with the token visible in `UserActivityLog.Path`.

This affects decorated endpoints even if they do not use request-body logging.

Recommendations:

1. Do not put credentials, access tokens, API keys, reset tokens, or personal data in query strings.
2. Avoid decorating endpoints that receive sensitive query values.
3. If query logging is required, extend the library or add a custom sanitization layer.
4. Restrict access to the audit table and apply short retention for sensitive records.
5. Review logs for existing leaked values and rotate any exposed credentials.

The entity's `Path` column is limited to 2000 characters. A longer path can cause the insert to fail; the failure is caught and logged.

---

## 6. `AdditionalData` handling

`AdditionalData` is a raw JSON string with no length limit in the model (`nvarchar(max)` on SQL Server). It is not redacted, validated, or size-capped.

When passing custom data:

```csharp
await _activityLogger.LogActionAsync(
    "Export report",
    new
    {
        ReportType = "Q4",
        RequestedBy = currentUserId
        // Do not include passwords, tokens, or sensitive personal data.
    });
```

Recommendations:

- Build a deliberate allow-list of fields.
- Do not serialize entire request models.
- Avoid circular references; serialization failures are logged and `AdditionalData` becomes null.
- Consider sanitizing before calling the logger.
- Ensure any UI that renders `AdditionalData` parses JSON and HTML-encodes values.

The library uses `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` for serialization. This makes JSON more readable but does not escape characters such as `<`, `>`, and `&`. Escaping on output is still required if the JSON is rendered in HTML.

---

## 7. IP address resolution and reverse proxies

### Default behavior

The service uses:

```csharp
context.Connection.RemoteIpAddress
```

with normalization:

- `::1` → `127.0.0.1`,
- IPv4-mapped IPv6 → IPv4,
- null → `"Unknown"`.

Behind a reverse proxy or load balancer, the remote address is usually the proxy, not the client.

### Recommended proxy setup

Use ASP.NET Core forwarded headers with a trusted-proxy policy:

```csharp
using Microsoft.AspNetCore.HttpOverrides;

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Configure KnownProxies/KnownNetworks for your deployment.
    // Do not trust arbitrary client-supplied forwarding headers.
});

var app = builder.Build();

app.UseForwardedHeaders();
```

Place `UseForwardedHeaders()` early in the pipeline, before components that consume the client IP.

### Custom resolver

```csharp
options.IpAddressResolver = context =>
{
    var ip = context.Connection.RemoteIpAddress?.ToString();
    return string.IsNullOrWhiteSpace(ip)
        ? "Unknown"
        : ip.Length <= 50
            ? ip
            : ip[..50];
};
```

If a custom resolver throws, the exception is not caught by the resolver call and can surface after result execution. If it returns a value longer than 50 characters, the database insert can fail.

Do not blindly trust `X-Forwarded-For` from clients. A spoofed header can misattribute audit events.

---

## 8. User identity

The service reads the claim configured by `UserIdClaimType`:

```csharp
options.UserIdClaimType = ClaimTypes.NameIdentifier;
```

Ensure authentication is configured and the claim is present. The middleware below assumes an authentication scheme has already been registered:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

For authenticated-only audit trails:

```csharp
options.LogAnonymousUsers = false;
```

When anonymous logging is disabled, requests without a non-empty user ID are skipped.

---

## 9. Persistence and transaction considerations

`LogAsync` calls `SaveChangesAsync()` on the application's scoped `DbContext`:

```csharp
_context.Set<UserActivityLog>().Add(log);
await _context.SaveChangesAsync();
```

This has security and correctness implications:

- All pending business changes in the same context are saved as well.
- Audit writes are not isolated from business transactions.
- A failure is caught and logged but not returned to the caller.
- The audit entity can remain tracked after a failed save.

For stronger guarantees, use a dedicated audit context, an outbox, or a background queue rather than writing inline through the business context.

---

## 10. Audit integrity and access control

The library stores ordinary database rows. It does not provide:

- append-only enforcement,
- hash chains or signatures,
- tamper detection,
- immutable/WORM storage,
- separate writer/reader database roles.

Operational recommendations:

- Restrict direct writes to the audit table where possible.
- Back up audit data according to policy.
- Retain records only as long as required.
- Monitor failed audit writes.
- Protect log data from unauthorized reads.
- Treat `AdditionalData` and paths as untrusted content when rendering.

---

## 11. Performance and volume

Every decorated request performs an inline database write. Consider:

- logging only meaningful events, not every GET,
- avoiding form logging for high-volume endpoints,
- reviewing index write overhead,
- adding retention/archival,
- using a background queue for high-traffic systems,
- monitoring connection-pool and database latency.

The library has no sampling, batching, cancellation, or retry support.

---

## 12. Recommended security checklist

- [ ] Keep secrets out of query strings.
- [ ] Do not log full forms unless the audit requirement justifies it.
- [ ] Expand `RedactedFields` for your domain.
- [ ] Verify redaction in a staging environment.
- [ ] Use a trusted forwarded-header configuration behind proxies.
- [ ] Set `LogAnonymousUsers = false` when only authenticated activity matters.
- [ ] Validate options at startup; avoid null arrays.
- [ ] Monitor `Failed to save user activity log`.
- [ ] Restrict and audit access to the log table.
- [ ] Define retention and deletion policies.
- [ ] HTML-encode `AdditionalData` when displaying it.
- [ ] Review the shared-`DbContext` behavior before using manual logging in business transactions.

---

## Related documentation

- [Getting Started](getting-started.md)
- [Usage Guide](usage-guide.md)
- [Architecture Overview](architecture-overview.md)
- [Troubleshooting](troubleshooting.md)
