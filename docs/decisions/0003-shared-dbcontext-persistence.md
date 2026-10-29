# ADR 0003: Synchronous Persistence Through Consuming DbContext

- **Status:** Accepted
- **Date:** 2026-10-21
- **Author:** UserActivityLogger Maintainers

## Context

Audit logging requires storing audit entities (`UserActivityLog`) into a persistent data store. The library must decide how to handle database access:
1. Reusing the consuming application's scoped `DbContext` (`TContext`).
2. Requiring an isolated dedicated `AuditDbContext` with separate connection string and pooling.
3. Writing to an asynchronous background outbox or message queue (e.g., RabbitMQ, Redis, Channel worker).

## Decision

Re-use the consuming application's registered `DbContext` by implementing `UserActivityLoggerService<TContext>` where `TContext : DbContext`:
- Consumers configure `builder.ConfigureUserActivityLogger()` in `OnModelCreating`.
- The library registers `UserActivityLoggerService<TContext>` as a scoped service.
- The service accesses `_context.Set<UserActivityLog>()` and calls `await _context.SaveChangesAsync()`.
- Exceptions during `SaveChangesAsync` are logged to `ILogger` and swallowed to prioritize request success.

## Consequences

### Positive
- Zero external infrastructure dependencies (no RabbitMQ, Redis, or background service configuration).
- Minimal configuration for consumers: a single generic call `AddUserActivityLogger<AppDbContext>()`.
- Directly inherits the transaction provider, connection strings, migrations, and pooling of the consuming application.

### Negative / Costs
- Shared Unit of Work: Calling `_context.SaveChangesAsync()` commits any pending tracked entity changes in the scoped `DbContext`.
- Audit write overhead runs inline on the request path, impacting latency.
- Best-effort persistence: If the database write fails, the error is caught and logged, but the audit entry is lost without automatic retry.
