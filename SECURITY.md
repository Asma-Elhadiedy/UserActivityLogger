# Security policy

This policy outlines how security vulnerabilities in UserActivityLogger are reported, triaged, and remediated.

## Supported versions

Security updates are provided for the active release line across supported target frameworks:

| Version | Supported target frameworks | Status |
|---|---|---|
| `1.0.x` | `net8.0`, `net9.0`, `net10.0` | Supported |
| `< 1.0.0` | None | Unsupported |

## Reporting a vulnerability

Do not report suspected security vulnerabilities through public GitHub issues or pull requests.

To report a vulnerability:
1. Use GitHub Private Vulnerability Reporting via the repository Security tab (Settings -> Code security and analysis -> Private vulnerability reporting must be enabled by the repository owner).
2. If Private Vulnerability Reporting is not yet enabled on the upstream repository, contact the repository maintainers via the email address listed on their GitHub profile.

### Required information

Include the following technical details to accelerate triage:
- UserActivityLogger package version (such as `1.0.10`).
- Target framework (`net8.0`, `net9.0`, or `net10.0`) and the Entity Framework Core provider in use.
- Detailed description of the vulnerability and security impact.
- Minimal reproduction code or step-by-step instructions.
- Any suggested remediation or patch.

### Response targets

- Initial acknowledgment: Within 48 hours of receipt.
- Severity triage and confirmation: Within 7 business days.
- Remediation: A patch release published to NuGet.org, followed by a GitHub Security Advisory.

## Scope

### In scope

- Redaction bypass: Failure of `UserActivityLoggingFilter` or `UserActivityLoggerOptions.RedactedFields` resulting in plaintext persistence of configured sensitive form fields.
- Resource exhaustion: Unbounded memory allocation or uncontrolled execution in form parsing or string formatting.
- Audit spoofing: Vulnerabilities allowing request actors to manipulate `UserId` or suppress audit rows on decorated endpoints.
- Injection: SQL or parameter injection introduced through `UserActivityLogger` model configurations or service calls.

### Out of scope

- Vulnerabilities in consuming host applications, web servers, or database instances.
- Vulnerabilities in third-party dependencies not directly exposed by UserActivityLogger APIs.
- Query string exposures: Query strings are appended to `Path` without redaction by design; consumers must not pass secrets in URLs.
- Direct database modification by actors with administrative database credentials.

## Consumer security recommendations

- Review redacted fields: Ensure all sensitive form fields used by your application are included in `UserActivityLoggerOptions.RedactedFields`.
- Do not pass secrets in query strings: The library stores the full query string in the `Path` column.
- Use secure proxy resolution: When operating behind reverse proxies, configure ASP.NET Core `ForwardedHeadersMiddleware` or supply a validated `IpAddressResolver` to prevent client IP spoofing.
- Restrict database permissions: Configure database user permissions so that application roles cannot alter or delete rows in `UserActivityLog`.
