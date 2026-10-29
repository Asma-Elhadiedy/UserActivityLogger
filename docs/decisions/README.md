# Architecture Decision Records (ADRs)

An Architecture Decision Record (ADR) captures an architectural decision made for `UserActivityLogger`, along with its context, consequences, and evaluated alternatives. ADRs are immutable historical records; if an earlier decision changes, a new ADR is created that supersedes the prior one.

## Index of Decisions

| ADR | Title | Status | Date |
|---|---|---|---|
| [0001](0001-multi-target-net8-net9-net10.md) | Multi-Targeting .NET 8, .NET 9, and .NET 10 | Accepted | 2026-10-21 |
| [0002](0002-mvc-result-filter-architecture.md) | MVC Result Filter for Action Interception | Accepted | 2026-10-21 |
| [0003](0003-shared-dbcontext-persistence.md) | Synchronous Persistence Through Consuming DbContext | Accepted | 2026-10-21 |

---

## When to Write an ADR

Submit an ADR when introducing:
- Changes to target framework support or major dependency versions.
- Changes to public interception mechanisms (e.g. middleware vs filters vs endpoint filters).
- Alterations to the persistence or data isolation model (e.g., dedicated context, background worker, outbox pattern).
- Breaking changes to public interfaces (`IUserActivityLogger`) or entity schemas (`UserActivityLog`).

---

## ADR Template

When creating a new ADR, use the format below:

```markdown
# ADR [Number]: [Title]

- **Status:** Proposed | Accepted | Superseded | Deprecated
- **Date:** YYYY-MM-DD
- **Author:** [Name or GitHub handle]

## Context

What problem are we solving? What forces, constraints, or trade-offs exist?

## Decision

What are we doing? Describe the technical implementation and design choice.

## Consequences

### Positive
- Benefits and improvements.

### Negative / Costs
- Trade-offs, limitations, or maintenance requirements.

## Alternatives Considered

- Alternative 1: Why was it rejected?
- Alternative 2: Why was it rejected?
```
