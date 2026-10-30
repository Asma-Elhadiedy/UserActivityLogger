# Changelog

All notable changes to UserActivityLogger are documented in this file.

The format is based on Keep a Changelog, and this project adheres to Semantic Versioning.

## [Unreleased]

### Added
- Isolated persistence model (`IsolatePersistence = true` by default) in `UserActivityLoggerOptions` and `UserActivityLoggerService<TContext>` to prevent audit writes from committing uncommitted business entities staged in the application's scoped unit of work.
- Dedicated `AuditDbContext` and `AddDedicatedUserActivityLogger` extension methods supporting independent audit databases or schemas via `IDbContextFactory<AuditDbContext>`.
- `AddIsolatedUserActivityLogger<TContext>` extension method for explicit isolation registration.
- Automated unit test suite under `tests/UserActivityLogger.UnitTests` covering DbContext isolation, dedicated contexts, and shared fallback behavior.
- Continuous integration workflow (`ci.yml`) covering multi-target build, packaging, code formatting, and link validation.
- Release workflow (`publish.yml`) with tag-to-version verification and automated publishing to NuGet.org.
- Community and governance policies: `CODE_OF_CONDUCT.md`, `SECURITY.md`, `SUPPORT.md`, and updated `CONTRIBUTING.md`.
- Shared compiler configuration in `Directory.Build.props` for deterministic CI builds.
- Code style definitions in `.editorconfig` and YAML lint configuration in `.yamllint.yml`.
- Local verification script `eng/verify.sh` and link/symbol checker `.github/scripts/check-docs.py`.
- Consolidated technical documentation under `docs/` covering getting started, usage, architecture, security, and troubleshooting.

## [1.0.10] - 2026-10-21

### Added
- Multi-targeting support for `net8.0`, `net9.0`, and `net10.0`.
- Declarative `[LogUserActivity]` attribute for ASP.NET Core MVC controllers and actions.
- Global `UserActivityLoggingFilter` result filter for automated post-result audit logging.
- `UserActivityLoggerOptions` with configuration for `RedactedFields`, `LogAnonymousUsers`, `SkipPaths`, and `IpAddressResolver`.
- Entity Framework Core model configuration extension `builder.ConfigureUserActivityLogger()`.
- Audit logging contract `IUserActivityLogger` and Entity Framework Core service `UserActivityLoggerService<TContext>`.
- Form request body capture with case-insensitive field name redaction.
- Predefined event description templates in `ConstPredefinedLogMessage`.
- Dependency injection extension methods `AddUserActivityLogger<TContext>` and `AddUserActivityLogging`.
