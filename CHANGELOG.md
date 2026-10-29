# Changelog

All notable changes to UserActivityLogger are documented in this file.

The format is based on Keep a Changelog, and this project adheres to Semantic Versioning.

## [Unreleased]

### Added
- Continuous integration and release workflows (`ci.yml`, `publish.yml`, `code-quality.yml`, `labeler.yml`).
- Governance and community policies: `CODE_OF_CONDUCT.md`, `SECURITY.md`, `SUPPORT.md`, and revised `CONTRIBUTING.md`.
- Architecture Decision Records framework under `docs/decisions/`.
- Local verification script `eng/verify.sh` and link/symbol checker `.github/scripts/check-docs.py`.
- Shared compiler configuration in `Directory.Build.props` for deterministic CI builds.
- Code style definitions in `.editorconfig` and YAML lint configuration in `.yamllint.yml`.

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

### Documentation
- Getting started guide covering installation, DbContext configuration, and migrations.
- Usage guide with examples for attributes, form capture, conditional event overrides, and manual logging.
- Architecture overview describing request pipeline lifecycle, entity schema, and indexes.
- Security and configuration guide detailing options, redaction rules, and proxy IP handling.
- Troubleshooting guide and common issue resolutions.
