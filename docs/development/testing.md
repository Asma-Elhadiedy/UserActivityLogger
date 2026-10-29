# Testing guide

This document describes the testing state of the repository, how to run the existing sample application, and the planned roadmap for automated test coverage.

## Current state

The solution contains two projects:
1. `UserActivityLogger`: The library project multi-targeting `net8.0`, `net9.0`, and `net10.0`.
2. `UserActivityLogger.Tests`: An ASP.NET Core MVC web application targeting `net9.0` (`Microsoft.NET.Sdk.Web`).

The `UserActivityLogger.Tests` project is an interactive sample application demonstrating integration with Entity Framework Core and SQL Server. The repository does not currently contain automated unit test projects (such as xUnit, NUnit, or MSTest) or continuous integration test runners.

Verification in CI currently consists of building the solution across all target frameworks, compiling the package, running static code formatting checks, and validating documentation links.

### Running the sample application

To verify library integration manually using the sample application:

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

4. Navigate to `http://localhost:<port>/Departments` and perform actions (Create, Edit, Delete).

5. Query the database table `UserActivityLog`:
   ```sql
   SELECT TOP (10) Id, Event, ResponseStatusCode, IPAddress, Path, Method, DateEvent, UserId
   FROM UserActivityLog
   ORDER BY DateEvent DESC;
   ```

Expected result:
Audit rows are present with non-zero response status codes, route paths, and timestamps.

## Testing roadmap

The following automated test coverage is planned for future releases:

### Unit testing

A dedicated test project using xUnit targeting `net8.0`, `net9.0`, and `net10.0` to verify isolated components:
- `IpAddressHelper`: Validation of IP address extraction with single IP, multiple comma-separated proxy IPs in `X-Forwarded-For`, IPv4, IPv6, and null context fallbacks.
- `UserActivityLoggerOptions`: Default configuration values, custom IP resolver assignment, and case-insensitive redaction matching.
- `ConstPredefinedLogMessage`: Format string validation using `{0}` (action) and `{1}` (controller).

### Integration testing

An integration test suite using `Microsoft.AspNetCore.Mvc.Testing` and SQLite In-Memory:
- Validate that endpoints with `[LogUserActivity]` persist records upon successful result execution.
- Validate that form fields matching `RedactedFields` are replaced with the configured placeholder.
- Validate that requests to paths matching `SkipPaths` produce no database records.
- Validate that non-form endpoints do not trigger request body parsing.
