# Changelog

All notable changes to `UserActivityLogger` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive GitHub workflow automation (`ci.yml`, `publish.yml`, `code-quality.yml`, `labeler.yml`).
- Community health files: `CODE_OF_CONDUCT.md`, `SECURITY.md`, `SUPPORT.md`, and updated `CONTRIBUTING.md`.
- Architecture Decision Records (ADRs) framework in `docs/decisions/`.
- Local verification script `eng/verify.sh` and Python documentation validator `.github/scripts/check-docs.py`.
- Repository `.editorconfig` and `Directory.Build.props` for deterministic CI builds.

## [1.0.10] - 2026-10-21

### Added
- Multi-targeting support for `.NET 8.0`, `.NET 9.0`, and `.NET 10.0`.
- Declarative `[LogUserActivity]` attribute for ASP.NET Core MVC controllers and actions.
- Global `UserActivityLoggingFilter` for automated post-result audit persistence.
- `UserActivityLoggerOptions` with customizable redaction (`RedactedFields`), anonymous user logging (`LogAnonymousUsers`), path skipping (`SkipPaths`), and custom `IpAddressResolver`.
- Entity Framework Core model configuration via `builder.ConfigureUserActivityLogger()`.
- Manual audit logging interface `IUserActivityLogger` with `LogAsync` and `LogActionAsync`.
- Form request-body logging with automatic redaction for sensitive fields.
- Predefined message templates and constants in `ConstPredefinedLogMessage`.
- Dependency injection extension methods `AddUserActivityLogger<TContext>` and MVC option `AddUserActivityLogging`.

### Documentation
- Getting Started guide covering installation, `DbContext`, and EF Core migrations.
- Complete Usage Guide with code examples for attributes, form capture, and conditional log messages.
- Architecture Overview detailing the request pipeline, data model, indexes, and design boundaries.
- Security and Configuration guide with redaction rules and IP proxy handling.
- Troubleshooting guide and FAQ.
