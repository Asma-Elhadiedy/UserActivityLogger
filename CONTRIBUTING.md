# Contributing to UserActivityLogger

Thanks for your interest in contributing! This document covers how to get set up, the coding conventions to follow, and how to submit changes.

## Getting Started

### Prerequisites

- .NET SDK 9.0
- Git

### Setup

```bash
# Clone the repo
git clone https://github.com/Asma-Elhadiedy/UserActivityLogger.git
cd UserActivityLogger

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test
```

## Project Structure

```
UserActivityLogger/
├── src/
│   └── UserActivityLogger/
│       ├── Attributes/       # LogUserActivityAttribute
│       ├── Entities/         # UserLog
│       ├── Extensions/       # DI registration extensions
│       ├── Filters/          # UserActivityLoggingFilter
│       ├── Helpers/          # IP address helpers, etc.
│       ├── Interfaces/       # IUserActivityLogger
│       └── Options/          # UserActivityLoggerOptions
│
│── tests/
│   └── UserActivityLogger.Tests
├── README.md
└── LICENSE
```

## How to Contribute

### Reporting Bugs

Open an issue and include:
- .NET version and OS
- Steps to reproduce
- Expected vs. actual behavior
- Stack trace, if applicable

### Suggesting Features

Open an issue describing the use case before submitting a PR — this avoids wasted effort if the feature doesn't fit the project's scope (lightweight, storage-agnostic activity logging).

### Submitting a Pull Request

1. Fork the repository
2. Create a branch from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. Make your changes
4. Add or update tests for any behavior change
5. Ensure the build and tests pass:
   ```bash
   dotnet build -c Release
   dotnet test
   ```
6. Commit with a clear message (see [Commit Messages](#commit-messages))
7. Push and open a pull request against `main`

## Coding Guidelines

- **Nullable reference types** are enabled — respect `?` annotations and avoid introducing warnings
- **XML doc comments** are required on all public types and members
- Keep the package **free of Entity Framework or any storage-specific dependency** — this is a core design constraint, not a preference. `IUserActivityLogger` implementations belong in consuming applications, not this package
- Match existing naming and file organization (one type per file, folder per concern)
- Avoid adding new NuGet dependencies unless necessary; keep the package lightweight

## Commit Messages

Use clear, present-tense messages. A good format is:

```
feature: add IP resolver fallback for missing X-Forwarded-For header
fix: fix redaction not applying to nested form keys
docs: update README with conditional logging example
test: add a test for the Track class
```


## Versioning

This project follows [Semantic Versioning](https://semver.org/):
- **Major** — breaking changes (interface signature changes, dropped framework support)
- **Minor** — new features, backward compatible
- **Patch** — bug fixes, backward compatible

Maintainers handle version bumps and publishing to NuGet.org as part of the release process — contributors don't need to update the package version in PRs.

## Code of Conduct

Be respectful and constructive. Disagreements on approach are fine; keep discussion focused on the code and the problem being solved.

## Questions

Open a [GitHub Discussion](https://github.com/Asma-Elhadiedy/UserActivityLogger/discussions) or an issue if anything here is unclear.