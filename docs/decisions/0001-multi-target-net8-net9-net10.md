# ADR 0001: Multi-targeting .NET 8, .NET 9, and .NET 10

- Status: Accepted
- Date: 2026-10-21
- Author: UserActivityLogger Maintainers

## Context

UserActivityLogger is consumed by ASP.NET Core applications across multiple .NET versions:
- .NET 8 is an active Long Term Support (LTS) release with enterprise adoption.
- .NET 9 is the Standard Term Support (STS) release.
- .NET 10 is the subsequent release.

Targeting a single framework version restricts consumer adoption and generates package dependency warnings when consumed by applications on different framework versions.

## Decision

Configure `UserActivityLogger.csproj` to multi-target:
```xml
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

Align `Microsoft.EntityFrameworkCore` package references conditionally per target framework:
- `net8.0`: `Microsoft.EntityFrameworkCore` 8.0.0
- `net9.0`: `Microsoft.EntityFrameworkCore` 9.0.0
- `net10.0`: `Microsoft.EntityFrameworkCore` 10.0.0

## Consequences

### Positive
- Applications targeting .NET 8, 9, or 10 can reference the package without assembly mismatch warnings.
- The library consumes the matching Entity Framework Core runtime for each target.

### Negative consequences
- CI pipelines must install and test against .NET 8, 9, and 10 SDKs.
- Library code must maintain API compatibility across all three target frameworks.

## Alternatives considered

- Target only .NET 9: Rejected because it blocks .NET 8 LTS applications from using the library.
- Target .NET Standard 2.0: Rejected because ASP.NET Core MVC and modern Entity Framework Core are delivered via framework references and modern .NET runtimes.
