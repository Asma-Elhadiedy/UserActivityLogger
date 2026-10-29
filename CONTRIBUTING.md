# Contributing to UserActivityLogger

This document covers local development setup, verification, testing, coding standards, commit conventions, pull request procedures, and release guidelines for UserActivityLogger.

By contributing to this project, you agree to adhere to the [Code of conduct](CODE_OF_CONDUCT.md).

## Getting started

### Prerequisites

- .NET SDK 8.0, 9.0, or 10.0 (the library multi-targets `net8.0`, `net9.0`, and `net10.0`)
- Git 2.30 or newer
- Python 3.10 or newer (for link and documentation validation)

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

The verification script reproduces CI checks locally: restoring dependencies, building in Release across all target frameworks, compiling the NuGet package, verifying code formatting, and validating documentation links.

## Testing with the sample application

The repository contains a sample ASP.NET Core MVC application (`UserActivityLogger.Tests`, targeting `net9.0`) demonstrating library integration with Entity Framework Core and SQL Server.

To run and verify the sample application locally:

1. Configure a database connection string in `UserActivityLogger.Tests/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=UserActivityLoggerDemo;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
   }
   ```

2. Apply the EF Core migration:
   ```bash
   dotnet ef database update --project UserActivityLogger.Tests
   ```

3. Start the application:
   ```bash
   dotnet run --project UserActivityLogger.Tests
   ```

4. Navigate to `http://localhost:<port>/Departments` and execute actions (Create, Edit, Delete).

5. Inspect the `UserActivityLog` table to verify audit entries are recorded with status codes, paths, and timestamps.

## Repository structure

```text
UserActivityLogger/
|-- .github/                  GitHub workflows, issue templates, and scripts
|   |-- ISSUE_TEMPLATE/       Issue form definitions
|   |-- scripts/              check-docs.py link and symbol validator
|   \-- workflows/            ci.yml and publish.yml workflows
|-- docs/                     Technical documentation
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

1. Search existing issues to verify the defect has not already been reported.
2. Submit a report using the Bug report issue form.
3. Include target framework (`net8.0`, `net9.0`, or `net10.0`), EF Core provider, minimal reproduction code, and expected versus observed behavior.

### Proposing enhancements

1. Open a Feature request issue before writing code for non-trivial additions.
2. Explain the use case, proposed API additions, and alternatives considered.
3. Confirm the proposal fits the library scope: lightweight audit logging for ASP.NET Core MVC applications.

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

- Target framework compatibility: UserActivityLogger multi-targets `net8.0`, `net9.0`, and `net10.0`. Do not use APIs introduced in newer .NET versions unless guarded with conditional compilation directives (`#if NET9_0_OR_GREATER`).
- Nullable reference types: Nullable reference types are enabled across all projects (`<Nullable>enable</Nullable>`). Do not introduce compiler warnings.
- XML documentation: All public types, methods, and properties must include valid XML documentation comments.
- Minimal dependencies: The package references `Microsoft.AspNetCore.App` and `Microsoft.EntityFrameworkCore`. Do not introduce third-party package dependencies.
- Public API stability: Preserve backward compatibility for existing types, interfaces, and method signatures in minor and patch releases.

## Commit conventions

Commit messages must follow the Conventional Commits specification:

- `feat`: A new feature or public capability. Example: `feat: add custom header support for IP resolver`.
- `fix`: A bug fix. Example: `fix: handle empty SkipPaths collection without exception`.
- `docs`: Documentation changes. Example: `docs: document proxy IP header configuration`.
- `test`: Modifications to the sample application. Example: `test: add department edit scenario`.
- `refactor`: Code restructuring without behavioral change. Example: `refactor: extract form key redaction helper`.
- `ci`: Workflow or automation script changes. Example: `ci: consolidate format check into build pipeline`.
- `chore`: Maintenance tasks, configuration updates, or dependency version bumps. Example: `chore: update editorconfig rules`.

## Release process (for maintainers)

UserActivityLogger follows Semantic Versioning 2.0.0 (MAJOR.MINOR.PATCH). The single source of truth for the package version is the `<Version>` tag in `UserActivityLogger/UserActivityLogger.csproj`.

To publish a new release:

1. Verify `master` passes all CI checks.
2. Update `CHANGELOG.md` by moving items from `[Unreleased]` to a new version heading with the current date.
3. Update `<Version>` in `UserActivityLogger/UserActivityLogger.csproj`.
4. Run `./eng/verify.sh` to ensure build, pack, format, and docs checks pass.
5. Commit and push to `master`:
   ```bash
   git commit -am "chore(release): bump version to 1.0.11"
   git push upstream master
   ```
6. Create an annotated git tag matching the version and push to upstream:
   ```bash
   git tag -a v1.0.11 -m "Release v1.0.11"
   git push upstream v1.0.11
   ```

The `publish.yml` workflow verifies that the git tag version matches the `.csproj` version, builds across all target frameworks, packages the library, pushes the package to NuGet.org with `--skip-duplicate`, and creates a GitHub Release with attached artifacts.

### Package unlisting

NuGet.org does not permit permanently deleting packages once published. To deprecate or unlist a faulty package version:

```bash
dotnet nuget delete UserActivityLogger <version> \
  --api-key "<NUGET_API_KEY>" \
  --non-interactive
```

Alternatively, uncheck "List in search results" under the Listing tab at `https://www.nuget.org/packages/UserActivityLogger/<version>/Manage`.
