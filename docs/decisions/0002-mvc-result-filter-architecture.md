# ADR 0002: MVC result filter for action interception

- Status: Accepted
- Date: 2026-10-21
- Author: UserActivityLogger Maintainers

## Context

Recording audit events requires discovering action metadata (`[LogUserActivity]`), capturing request parameters, evaluating the final response status code, and persisting an audit record.

ASP.NET Core provides three primary interception mechanisms:
- HTTP middleware: Executes outside the MVC pipeline.
- MVC action filter (`IAsyncActionFilter`): Executes before the action result produces an HTTP response.
- MVC result filter (`IAsyncResultFilter`): Executes around action result execution (view rendering or response serialization).

## Decision

Implement `UserActivityLoggingFilter` as an `IAsyncResultFilter` registered globally in MVC options:
- Inspect endpoint metadata for `LogUserActivityAttribute`.
- Capture form body data before result execution if request body logging is enabled.
- Execute `await next()` to complete action result processing.
- Read `Response.StatusCode` and invoke `IUserActivityLogger.LogActionAsync`.

## Consequences

### Positive
- Captures the final HTTP status code produced after result execution.
- Allows action methods to pass conditional overrides via `HttpContext.Items["ConditionalLogAction"]`.
- Integrates directly with ASP.NET Core MVC endpoint routing metadata.

### Negative consequences
- Unhandled exceptions thrown during action execution bypass result execution and are not recorded by the filter.
- Endpoints outside MVC (such as Minimal APIs and static files) do not run this filter.

## Alternatives considered

- HTTP middleware: Rejected because middleware does not have native access to MVC action descriptor route values (`action`, `controller`) or endpoint metadata without custom endpoint resolution logic.
- MVC action filter: Rejected because action filters execute before the `IActionResult` completes, leaving the final HTTP status code incomplete.
