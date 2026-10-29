# Testing Guide

This guide explains how `UserActivityLogger` is tested, how to run the sample application, and how to verify changes.

---

## Current Architecture: Sample Test Project

The repository includes a project named `UserActivityLogger.Tests/`. In version 1.0.10, this project is configured as an ASP.NET Core MVC Web Application (`Microsoft.NET.Sdk.Web`). It serves as an interactive sample application demonstrating:
- DbContext configuration with `builder.ConfigureUserActivityLogger()`.
- Service registration with `AddUserActivityLogger<ApplicationDbContext>()`.
- Filter registration with `options.AddUserActivityLogging()`.
- Attribute decoration on MVC controllers (`DepartmentsController` and `HomeController`).

### Running the Sample Application

To run the sample web application:

1. Update the database connection string in `UserActivityLogger.Tests/appsettings.json` (or `appsettings.Development.json`) to point to a local SQL Server instance or LocalDB.
2. Apply EF Core migrations:
   ```bash
   dotnet ef database update --project UserActivityLogger.Tests
   ```
3. Run the web application:
   ```bash
   dotnet run --project UserActivityLogger.Tests
   ```
4. Navigate to `https://localhost:<port>/Departments` to trigger audit logging.
5. Inspect the `UserActivityLog` table to confirm audit records are created with IP address, user identifier, response status code, and timestamps.

---

## Planned Automated Testing Strategy

While manual verification with the sample application validates end-to-end integration, adding automated unit and integration tests is on the roadmap:

### 1. Unit Tests (xUnit)
- `IpAddressHelperTests`: Test IP extraction with and without `X-Forwarded-For`, multiple IP commas, IPv4, IPv6, and null contexts.
- `UserActivityLoggerOptionsTests`: Test default options, redaction lists, and custom resolver assignment.
- `ConstPredefinedLogMessageTests`: Test string formatting with `{0}` (action) and `{1}` (controller).

### 2. Integration Tests (`Microsoft.AspNetCore.Mvc.Testing`)
- Use `WebApplicationFactory<Program>` with an in-memory or SQLite database to verify:
  - Attributes trigger logging.
  - Form parameters are redacted according to `RedactedFields`.
  - Non-form endpoints do not attempt body parsing.
  - Skip paths bypass logging.
