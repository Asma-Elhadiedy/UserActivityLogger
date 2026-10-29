# Architecture decision records

An Architecture Decision Record (ADR) captures a technical or architectural decision for UserActivityLogger, along with its context, consequences, and alternatives considered. ADRs are immutable. If an earlier decision changes, a new ADR is created that supersedes the prior one.

## Index of decisions

| ADR | Title | Status | Date |
|---|---|---|---|
| [0001](0001-multi-target-net8-net9-net10.md) | Multi-targeting .NET 8, .NET 9, and .NET 10 | Accepted | 2026-10-21 |
| [0002](0002-mvc-result-filter-architecture.md) | MVC result filter for action interception | Accepted | 2026-10-21 |
| [0003](0003-shared-dbcontext-persistence.md) | Synchronous persistence through consuming DbContext | Accepted | 2026-10-21 |

## When to write an ADR

Submit an ADR when introducing:
- Changes to target framework support or framework dependencies.
- Changes to request interception mechanisms (filters, middleware, or endpoint filters).
- Changes to the persistence architecture (isolated DbContext, outbox pattern, or background channels).
- Breaking changes to public interfaces (`IUserActivityLogger`), attributes, or entity schemas (`UserActivityLog`).

## ADR template

Use this template when creating an ADR:

```markdown
# ADR [number]: [title in sentence case]

- Status: Proposed | Accepted | Superseded | Deprecated
- Date: YYYY-MM-DD
- Author: [Name or GitHub handle]

## Context

Describe the problem, requirements, constraints, and forces involved.

## Decision

Describe the chosen approach and its technical implementation.

## Consequences

### Positive
- Benefits and operational improvements.

### Negative consequences
- Costs, constraints, or new maintenance responsibilities.

## Alternatives considered

- Alternative 1: Reason for rejection.
- Alternative 2: Reason for rejection.
```
