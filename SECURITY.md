# Security policy

This policy outlines how security vulnerabilities in UserActivityLogger are reported and handled.

## Supported versions

Security updates are provided for the active release line:

| Version | Supported target frameworks | Status |
|---|---|---|
| `1.0.x` | `net8.0`, `net9.0`, `net10.0` | Supported |
| `< 1.0.0` | None | Unsupported |

## Reporting a vulnerability

Do not report security vulnerabilities through public GitHub issues or discussions.

To report a vulnerability:
1. Submit a report through GitHub Private Vulnerability Reporting on the repository Security tab (requires Private Vulnerability Reporting to be enabled by the repository owner under Settings -> Code security and analysis).
2. Alternatively, contact the maintainers directly via the email listed on their GitHub profile.

Please include the package version, target framework, EF Core provider, and minimal steps or code to reproduce the issue. Confirmed vulnerabilities will be addressed in a patch release published to NuGet.org.
