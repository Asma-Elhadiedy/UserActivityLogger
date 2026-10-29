# Contributor getting started guide

This guide covers environment setup, build instructions, and local verification steps for contributing to UserActivityLogger.

## Prerequisites

Building the solution requires:
- .NET SDK 8.0, 9.0, or 10.0. The library multi-targets `net8.0`, `net9.0`, and `net10.0`. Installing the .NET 10 SDK or targeting packs for all three frameworks is recommended.
- Git 2.30 or newer.
- Python 3.10 or newer (for documentation and link validation).

Verify installed SDKs:
```bash
dotnet --list-sdks
```

## Clone and build

1. Clone the repository:
   ```bash
   git clone https://github.com/Asma-Elhadiedy/UserActivityLogger.git
   cd UserActivityLogger
   ```

2. Restore dependencies:
   ```bash
   dotnet restore UserActivityLogger.sln
   ```

3. Build the solution in Release configuration:
   ```bash
   dotnet build UserActivityLogger.sln -c Release --no-restore
   ```

Expected output:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Run verification script

The repository provides a shell script that reproduces the continuous integration checks locally:

```bash
./eng/verify.sh
```

The script executes the following steps:
1. Restores all solution dependencies via `dotnet restore UserActivityLogger.sln`.
2. Builds the solution in Release configuration via `dotnet build UserActivityLogger.sln -c Release --no-restore`.
3. Packs the library via `dotnet pack UserActivityLogger/UserActivityLogger.csproj -c Release --no-build`.
4. Runs code style and whitespace verification via `dotnet format`.
5. Validates Markdown links and verifies absence of emojis via `python3 .github/scripts/check-docs.py`.

## Package generation

To build the NuGet package manually without the verification script:

```bash
dotnet pack UserActivityLogger/UserActivityLogger.csproj -c Release --output ./artifacts
```

The compiled package is placed in `./artifacts/UserActivityLogger.<version>.nupkg`.

## Common failure modes

- Missing targeting pack (NETSDK1127): If building a specific target framework fails with `The targeting pack Microsoft.NETCore.App is not installed`, install the corresponding .NET SDK runtime or run `dotnet restore` with network access.
- Code formatting error: If `dotnet format` reports discrepancies, run `dotnet format style` or review changes against `.editorconfig`.
- Documentation link error: If `check-docs.py` reports a broken link, verify that the relative file path exists and does not reference `.local/`.
