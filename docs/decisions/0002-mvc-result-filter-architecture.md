# ADR 0002: MVC Result Filter for Action Interception

- **Status:** Accepted
- **Date:** 2026-10-21
- **Author:** UserActivityLogger Maintainers

## Context

To record user activity and audit logs in an ASP.NET Core MVC application, the library needs to intercept requests, read endpoint metadata (`[LogUserActivity]`), extract request details, determine the response status code, and record the audit entry.

ASP.NET Core offers multiple interception mechanisms:
1. HTTP Middleware: Operates outside the MVC pipeline; runs for every HTTP request before routing is finalized.
2. MVC Action Filter (`IAsyncActionFilter`): Operates around the action method before the result (`IActionResult`) is executed.
3. MVC Result Filter (`IAsyncResultFilter`): Operates around the result execution (`ViewResult`, `JsonResult`, etc.) after the action has produced its result.

## Decision

Implement `UserActivityLoggingFilter` as an `IAsyncResultFilter` registered globally via MVC options:
- Intercepts requests that have `LogUserActivityAttribute` in endpoint metadata.
- Pre-captures form body if configured before executing the result.
- Executes `await next()` to render or serialize the result.
- Reads `HttpContext.Response.StatusCode` and writes the audit log via `IUserActivityLogger`.

## Consequences

### Positive
- Captures the true final HTTP status code produced by the action result execution.
- Enables actions to pass runtime contextual messages or conditional overrides (`HttpContext.Items["ConditionalLogAction"]`).
- Seamlessly leverages ASP.NET Core MVC endpoint routing metadata.

### Negative / Costs
- Unhandled exceptions thrown during action execution bypass the result filter; unhandled failures are therefore not automatically logged by the filter path alone.
- Non-MVC endpoints (such as Minimal APIs, static files, or middleware short-circuits) are not captured by this filter.
