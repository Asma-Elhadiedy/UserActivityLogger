# UserActivityLogger Documentation Index

Welcome to the documentation for **UserActivityLogger**, a lightweight, attribute-driven user activity and audit logging library for ASP.NET Core MVC applications.

This directory contains user guides, architecture specifications, security guidelines, troubleshooting documents, and architecture decision records.

---

## 📚 Guides for Consumers

| Document | Description |
|---|---|
| [Getting Started](getting-started.md) | Installation, initial `DbContext` configuration, DI registration, and first migration. |
| [Usage Guide](usage-guide.md) | Declarative action logging, predefined event messages, form body logging, conditional messages, and manual logging. |
| [Security & Configuration](security-and-configuration.md) | Configuration options, PII redaction rules, reverse proxy IP handling, and data privacy. |
| [Troubleshooting & FAQ](troubleshooting.md) | Common setup pitfalls, migration issues, missing audit rows, and runtime diagnostics. |

---

## 🏛️ Architecture & Decisions

| Document | Description |
|---|---|
| [Architecture Overview](architecture-overview.md) | Pipeline execution model, filter vs service roles, entity model, database indexes, and known constraints. |
| [Architecture Decision Records (ADRs)](decisions/README.md) | Record of architectural decisions, context, consequences, and evaluated alternatives. |

---

## 🛠️ Contributor & Developer Documentation

| Document | Description |
|---|---|
| [Contributor Getting Started](development/getting-started.md) | Local development setup, building multi-target frameworks, and tools. |
| [Testing Guide](development/testing.md) | How the sample application works, manual verification, and guidelines for automated testing. |
| [Release & Publishing Guide](development/releasing.md) | Semantic versioning, release tags, NuGet publishing workflow, and release checklists. |

---

## 🤝 Project Policies

- [Contributing Guidelines](../CONTRIBUTING.md) — How to submit pull requests, coding standards, and commit conventions.
- [Code of Conduct](../CODE_OF_CONDUCT.md) — Community standards and enforcement.
- [Security Policy](../SECURITY.md) — Vulnerability reporting and scope.
- [Support Guidelines](../SUPPORT.md) — Where to ask questions and get help.
- [Changelog](../CHANGELOG.md) — Version history and release notes.
