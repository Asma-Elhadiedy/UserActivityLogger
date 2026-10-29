# Contributing to UserActivityLogger

This document covers local development setup, coding standards, commit conventions, and the pull request process for UserActivityLogger.

By contributing to this project, you agree to adhere to the [Code of conduct](CODE_OF_CONDUCT.md).

## Getting started

### Prerequisites

- .NET SDK 8.0, 9.0, or 10.0
- Git 2.30 or newer
- Python 3.10 or newer (for documentation checks)

### Development setup

1. Fork the repository and clone your fork:
   ```bash
   git clone https://github.com/<your-username>/UserActivityLogger.git
   cd UserActivityLogger
   ```

2. Restore dependencies:
   ```bash
   dotnet restore UserActivityLogger.sln
   ```

3. Build the solution:
   ```bash
   dotnet build UserActivityLogger.sln -c Release --no-restore
   ```

4. Run the local verification script:
   ```bash
   ./eng/verify.sh
   ```

## Repository structure

```text
UserActivityLogger/
|-- .github/                  GitHub workflows, issue templates, and scripts
|   |-- ISSUE_TEMPLATE/       Issue form definitions
|   |-- scripts/              check-docs.py link and symbol validator
|   \-- workflows/            Continuous integration and publish workflows
|-- docs/                     Technical documentation
|   |-- decisions/            Architecture Decision Records (ADRs)
|   |-- development/          Setup, testing, and release procedures
|   |-- architecture-overview.md
|   |-- getting-started.md
|   |-- security-and-configuration.md
|   |-- troubleshooting.md
|   \-- usage-guide.md
|-- eng/                      verify.sh local verification script
|-- UserActivityLogger/       Library project multi-targeting net8.0, net9.0, net10.0
|   |-- Attributes/           LogUserActivityAttribute
|   |-- Constants/            Predefined event template strings
|   |-- Entities/             UserActivityLog entity definition
|   |-- Extensions/           ServiceCollection and ModelBuilder extensions
|   |-- Filters/              UserActivityLoggingFilter result filter
|   |-- Helpers/              IpAddressHelper
|   |-- Options/              UserActivityLoggerOptions
|   \-- Services/             IUserActivityLogger and UserActivityLoggerService
|-- UserActivityLogger.Tests/ Sample ASP.NET Core MVC application (net9.0)
|-- CHANGELOG.md              Release history
|-- CODE_OF_CONDUCT.md        Community standards
|-- Directory.Build.props     Deterministic build properties
|-- LICENSE                   MIT license
|-- README.md                 Repository overview
|-- SECURITY.md               Vulnerability reporting policy
\-- SUPPORT.md                Support policy and channels
```

## How to contribute

### Reporting bugs

1. Search existing issues to verify the defect has not been reported.
2. Use the Bug report issue form.
3. Provide target framework (`net8.0`, `net9.0`, or `net10.0`), Entity Framework Core provider name and version, minimal reproduction steps, and expected versus observed behavior.

### Proposing enhancements

Before writing code for non-trivial additions:
1. Open a Feature request issue.
2. Describe the problem, the proposed API additions, and alternatives considered.
3. Ensure the feature fits the library boundary: lightweight audit logging for ASP.NET Core MVC applications.

### Submitting a pull request

1. Create a focused branch from the current `master` branch:
   ```bash
   git checkout -b feat/your-feature-name
   ```
2. Implement your changes adhering to existing code conventions.
3. Validate locally:
   ```bash
   ./eng/verify.sh
   ```
4. Commit your changes using Conventional Commits.
5. Push to your fork and submit a pull request against `master`.
6. Complete all checklist items in the pull request template.

## Coding guidelines

- Target framework compatibility: UserActivityLogger multi-targets `net8.0`, `net9.0`, and `net10.0`. Do not use APIs introduced in .NET 9 or .NET 10 unless guarded with conditional compilation directives (`#if NET9_0_OR_GREATER`).
- Nullable reference types: Nullable reference types are enabled across all projects (`<Nullable>enable</Nullable>`). Do not introduce compiler warnings.
- XML documentation: All public types, methods, and properties must include valid XML documentation comments.
- Minimal dependencies: The package references `Microsoft.AspNetCore.App` and `Microsoft.EntityFrameworkCore`. Do not introduce third-party package dependencies without an accepted Architecture Decision Record.
- Public API stability: Preserve backward compatibility for existing types, interfaces, and method signatures in minor and patch releases.

## Commit conventions

Commit messages must follow the Conventional Commits specification:

- `feat`: A new feature or public capability. Example: `feat: add custom header support for IP resolver`.
- `fix`: A bug fix. Example: `fix: handle empty SkipPaths collection without exception`.
- `docs`: Documentation changes. Example: `docs: document proxy IP header configuration`.
- `test`: Additions or modifications to the sample application. Example: `test: add department edit scenario`.
- `refactor`: Code restructuring without behavioral change. Example: `refactor: extract form key redaction helper`.
- `ci`: Workflow or automation script changes. Example: `ci: add actionlint step`.
- `chore`: Maintenance tasks, configuration updates, or dependency version bumps. Example: `chore: update editorconfig rules`.

## Documentation guidelines

- Keep documents in `docs/` synchronized with code changes.
- Significant technical decisions require an ADR under `docs/decisions/` following the template in `docs/decisions/README.md`.
- Verify all links pass `./.github/scripts/check-docs.py`.

## Release and versioning

This project adheres to Semantic Versioning 2.0.0:
- MAJOR: Incompatible public API changes.
- MINOR: Backward-compatible functionality additions.
- PATCH: Backward-compatible bug fixes.

Maintainers handle version updates in `UserActivityLogger.csproj`, changelog updates, and git tagging. Do not modify the package version in contribution pull requests.
