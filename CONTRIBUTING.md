# Contributing to UserActivityLogger

Thank you for your interest in contributing to `UserActivityLogger`! As an open-source .NET library, we welcome contributions ranging from bug fixes and documentation improvements to new features.

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

---

## Getting Started

### Prerequisites

- [.NET SDK 8.0, 9.0, or 10.0](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/)
- Python 3.10+ (optional, for local documentation validation)

### Development Setup

```bash
# Clone your fork
git clone https://github.com/<your-username>/UserActivityLogger.git
cd UserActivityLogger

# Restore dependencies
dotnet restore

# Build solution in Release configuration
dotnet build -c Release

# Run verification script
./eng/verify.sh
```

---

## Repository Structure

```text
UserActivityLogger/
├── .github/                  # GitHub workflows, templates, labeler, scripts
│   ├── ISSUE_TEMPLATE/       # Issue form templates (bug, feature, docs, question)
│   ├── scripts/              # Documentation link verification script
│   └── workflows/            # GitHub Actions (CI, publish, code quality, labeler)
├── docs/                     # Comprehensive documentation
│   ├── decisions/            # Architecture Decision Records (ADRs)
│   ├── development/          # Contributor development, testing, and release guides
│   ├── architecture-overview.md
│   ├── getting-started.md
│   ├── security-and-configuration.md
│   ├── troubleshooting.md
│   └── usage-guide.md
├── eng/                      # Engineering scripts (verify.sh)
├── UserActivityLogger/       # The core NuGet library project (.NET 8/9/10)
│   ├── Attributes/           # [LogUserActivity]
│   ├── Constants/            # Predefined log message constants
│   ├── Entities/             # UserActivityLog entity
│   ├── Extensions/           # ServiceCollection & ModelBuilder extensions
│   ├── Filters/              # UserActivityLoggingFilter (MVC result filter)
│   ├── Helpers/              # IpAddressHelper
│   ├── Options/              # UserActivityLoggerOptions
│   └── Services/             # IUserActivityLogger & UserActivityLoggerService
├── UserActivityLogger.Tests/ # Sample ASP.NET Core MVC application demonstrating usage
├── CHANGELOG.md              # Keep a Changelog history
├── CODE_OF_CONDUCT.md        # Contributor Covenant Code of Conduct
├── Directory.Build.props     # Common build properties & deterministic builds
├── LICENSE                   # MIT License
├── README.md                 # Project overview and quick start
├── SECURITY.md               # Security policy and vulnerability disclosure
└── SUPPORT.md                # Support channels and guidelines
```

---

## How to Contribute

### Reporting Bugs

1. Search [existing issues](https://github.com/Asma-Elhadiedy/UserActivityLogger/issues) to ensure the bug hasn't already been reported.
2. Use the **Bug report** template.
3. Include your TargetFramework (`net8.0`, `net9.0`, or `net10.0`), EF Core provider, clear reproduction steps, and expected vs. actual behavior.

### Proposing Enhancements

Before writing large amounts of code:
1. Open a **Feature request** issue.
2. Describe the problem, the proposed API or behavior, and any alternatives considered.
3. Discuss with maintainers to ensure the feature fits the library's scope (lightweight, zero-ceremony audit logging for ASP.NET Core MVC).

### Submitting a Pull Request

1. Fork the repository and create a branch from `master`:
   ```bash
   git checkout -b feat/your-feature-name
   ```
2. Make your changes adhering to existing coding conventions.
3. Validate locally:
   ```bash
   ./eng/verify.sh
   ```
4. Commit your changes using [Conventional Commits](#commit-conventions).
5. Push to your fork and submit a Pull Request against `master`.
6. Fill in the Pull Request template thoroughly.

---

## Coding Guidelines

- **Target Compatibility:** The library multi-targets `net8.0`, `net9.0`, and `net10.0`. Avoid using language or framework APIs that are unavailable in .NET 8 unless guarded by conditional compilation `#if NET9_0_OR_GREATER`.
- **Nullable Reference Types:** Nullable annotations are enabled across the project (`<Nullable>enable</Nullable>`). Do not introduce compiler warnings.
- **XML Documentation:** All public types, methods, and properties must include accurate XML doc comments.
- **Lightweight Dependencies:** The package relies on `Microsoft.AspNetCore.App` (framework reference) and `Microsoft.EntityFrameworkCore`. Avoid introducing unnecessary external dependencies to keep the package lightweight.
- **Preserve Public API Compatibility:** Do not introduce breaking changes to existing public interfaces (`IUserActivityLogger`), attributes, or options without an approved Architectural Decision Record (ADR) and a major version bump plan.

---

## Commit Conventions

We follow [Conventional Commits](https://www.conventionalcommits.org/):

| Type | Description | Example |
|---|---|---|
| `feat` | A new feature or public capability | `feat: support custom claim type for tenant id` |
| `fix` | A bug fix | `fix: prevent NullReferenceException when SkipPaths is empty` |
| `docs` | Documentation changes | `docs: add example for IP resolver in load balancer setup` |
| `test` | Adding or updating tests or sample app | `test: add sample scenario for conditional logging` |
| `refactor` | Code restructuring without behavioral change | `refactor: extract form key normalization helper` |
| `ci` | Changes to CI workflows or scripts | `ci: add matrix test build for .NET 10` |
| `chore` | Maintenance tasks, repository configs | `chore: update editorconfig rules` |

---

## Documentation Guidelines

- Keep all documentation in `docs/` synchronized with code changes.
- If you make a significant architectural or design decision, add an ADR under `docs/decisions/` following the template in `docs/decisions/README.md`.
- Verify that relative links work and pass `./.github/scripts/check-docs.py`.

---

## Release & Versioning

This project adheres to [Semantic Versioning (SemVer)](https://semver.org/):
- **MAJOR** (`X.0.0`): Breaking API changes.
- **MINOR** (`1.X.0`): New backward-compatible functionality.
- **PATCH** (`1.0.X`): Backward-compatible bug fixes and optimizations.

Maintainers manage version bumps and automated publishing to NuGet.org upon tagging. Contributors do not need to increment package versions in PRs.
