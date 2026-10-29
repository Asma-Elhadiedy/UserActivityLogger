# Security Policy

The `UserActivityLogger` team takes the security of audit and user activity logging seriously. This policy outlines how security vulnerabilities should be reported and handled.

## Supported Versions

We provide security updates for the current active release line across supported .NET versions:

| Version | Supported Target Frameworks | Status |
|---|---|---|
| `1.0.x` | `.NET 8.0`, `.NET 9.0`, `.NET 10.0` | Supported |
| `< 1.0.0` | None | Unsupported |

## Reporting a Vulnerability

**Do not report security vulnerabilities through public GitHub issues.**

If you discover a security vulnerability in `UserActivityLogger`, please report it privately:

1. Use GitHub's **Private Vulnerability Reporting** feature via the repository's **Security** tab -> **Report a vulnerability**.
2. If private reporting is unavailable, reach out to the project maintainers via the contact email on their GitHub profile.

### What to Include in a Report

To help us triage and resolve the issue quickly, please provide:

- Package version affected (e.g., `1.0.10`).
- Target framework (`net8.0`, `net9.0`, or `net10.0`) and database provider in use.
- A clear description of the vulnerability and its potential impact.
- Step-by-step instructions or a minimal reproducible example.
- Any proposed fix or mitigation, if available.

### What to Expect

- **Acknowledgment:** We aim to acknowledge receipt of security reports within 48 hours.
- **Triage & Assessment:** We will assess severity and confirm impact within 7 business days.
- **Remediation & Advisory:** Once fixed, a patched NuGet package will be published, and a GitHub Security Advisory (CVE) will be released with credit to the reporter.

## Scope

### In Scope
- Sensitive data leaks: Failure of the redaction mechanism leading to plain-text storage of passwords, tokens, or configured secret keys.
- Denial of Service (DoS): Unbounded memory consumption or uncontrolled resource usage in `UserActivityLoggingFilter` or form parsing.
- Audit integrity / bypass: Bugs that allow an attacker to forge `UserId`, spoof audit event status, or bypass logging where explicitly marked.
- Injection vulnerabilities: Any scenario where logging input causes unexpected query manipulation or injection in supported EF Core providers.

### Out of Scope
- Vulnerabilities in consuming applications, hosting infrastructure, or operating systems.
- Vulnerabilities in third-party dependencies (unless directly exposed by `UserActivityLogger` APIs).
- Issues arising from storing sensitive information in URL query strings (the library explicitly documents that query strings are stored unredacted in `Path`).
- Direct database tampering by individuals with administrative database access.

## Best Practices for Consumers

- **Configure Redaction:** Review `UserActivityLoggerOptions.RedactedFields` and add all sensitive keys used in your application forms.
- **Avoid Secrets in URLs:** Never pass authentication tokens, passwords, or PII in query parameters.
- **Secure Proxies:** When operating behind a reverse proxy or load balancer, configure a secure `IpAddressResolver` or ASP.NET Core `ForwardedHeadersMiddleware` to prevent IP spoofing.
- **Restrict Audit Access:** Ensure that the `UserActivityLog` table in your database has appropriate read-only or restricted permissions for standard application roles.
