# ADR 0001: Multi-Targeting .NET 8, .NET 9, and .NET 10

- **Status:** Accepted
- **Date:** 2026-10-21
- **Author:** UserActivityLogger Maintainers

## Context

`UserActivityLogger` is a reusable NuGet library consumed by ASP.NET Core MVC applications. Consumers operate across different .NET lifecycle stages:
- .NET 8 is an active Long Term Support (LTS) release with broad enterprise adoption.
- .NET 9 is the Standard Term Support (STS) release widely used in modern applications.
- .NET 10 is the upcoming LTS release.

If the library targeted only `.NET 9`, projects running on `.NET 8` LTS would be unable to consume it without upgrading their entire application stack, and projects moving to `.NET 10` would encounter dependency warnings or incompatibilities.

## Decision

Configure the library project `UserActivityLogger.csproj` to multi-target:
```xml
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```
Match each target framework with its corresponding version of `Microsoft.EntityFrameworkCore`:
- For `net8.0`: `Microsoft.EntityFrameworkCore 8.0.0`
- For `net9.0`: `Microsoft.EntityFrameworkCore 9.0.0`
- For `net10.0`: `Microsoft.EntityFrameworkCore 10.0.0`

## Consequences

### Positive
- Broad ecosystem reach: Applications targeting .NET 8, 9, or 10 can install the package seamlessly.
- Avoids assembly binding and framework mismatch warnings for consumers.
- Native alignment with the consumer's EF Core runtime version.

### Negative / Costs
- CI pipelines must install and validate multiple .NET SDK versions.
- Code written in `UserActivityLogger` must either restrict itself to C# / BCL APIs available in .NET 8 or use conditional preprocessor directives (`#if NET9_0_OR_GREATER`).
