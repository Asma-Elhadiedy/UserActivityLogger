# ADR 0003: Synchronous persistence through consuming DbContext

- Status: Accepted
- Date: 2026-10-21
- Author: UserActivityLogger Maintainers

## Context

Audit records must be persisted into a relational database using Entity Framework Core. Three storage models were considered:
- Reusing the application's scoped `DbContext` (`TContext`).
- Introducing an isolated `AuditDbContext` with separate connection pooling and credentials.
- Publishing records to an outbox table or message broker for asynchronous processing.

## Decision

Reuse the consuming application's registered `DbContext` via `UserActivityLoggerService<TContext>`:
- The consumer configures the entity in `OnModelCreating` via `builder.ConfigureUserActivityLogger()`.
- The service accesses `_context.Set<UserActivityLog>()` and invokes `await _context.SaveChangesAsync()`.
- Persistence exceptions are logged via `ILogger` and swallowed to prevent logging failures from failing the primary HTTP request.

## Consequences

### Positive
- Requires no external message brokers or separate database connection pools.
- Consumers configure logging with a single method call: `services.AddUserActivityLogger<AppDbContext>()`.
- Directly shares the provider, connection strings, migrations, and database transactions configured in the host application.

### Negative consequences
- Shared unit of work: Calling `_context.SaveChangesAsync()` persists all pending entity changes tracked in the scoped `DbContext`.
- Inline persistence: Database roundtrips execute synchronously on the request thread.
- Best-effort persistence: If the database write fails, the error is logged and the audit record is lost without an automated retry mechanism.

## Alternatives considered

- Separate audit DbContext: Rejected for basic scenarios because it requires configuring secondary connection strings, connection pooling, and separate migration tooling.
- Message broker (RabbitMQ, Redis, Kafka): Rejected because it adds external infrastructure requirements for a lightweight NuGet package.
