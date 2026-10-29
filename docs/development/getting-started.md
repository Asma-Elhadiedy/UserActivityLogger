# Contributor Getting Started Guide

This guide helps contributors and maintainers set up their local environment to develop, build, and test `UserActivityLogger`.

---

## 1. Prerequisites

To build and run the complete solution, ensure you have:
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) and [.NET SDK 9.0 / 10.0](https://dotnet.microsoft.com/download) installed.
- [Git](https://git-scm.com/) installed and configured.
- A modern code editor: Visual Studio 2022 (v17.8+), Visual Studio Code with the C# Dev Kit extension, or JetBrains Rider.
- Optional: Python 3.10+ for documentation validation.

Verify installed SDKs:
```bash
dotnet --list-sdks
```

---

## 2. Clone and Build

1. Clone the repository:
   ```bash
   git clone https://github.com/Asma-Elhadiedy/UserActivityLogger.git
   cd UserActivityLogger
   ```

2. Restore NuGet dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution across all target frameworks:
   ```bash
   dotnet build UserActivityLogger.sln -c Release
   ```

---

## 3. Running Verification Locally

We provide an automated verification script that mirrors CI checks:

```bash
./eng/verify.sh
```

This script:
1. Builds the entire solution in `Release` configuration.
2. Generates the NuGet package (`.nupkg` and `.snupkg`).
3. Runs `dotnet format --verify-no-changes` to ensure adherence to `.editorconfig`.
4. Runs `.github/scripts/check-docs.py` to ensure all documentation links resolve.

---

## 4. Packaging the Library

To manually create the NuGet package:

```bash
dotnet pack UserActivityLogger/UserActivityLogger.csproj -c Release
```

The resulting package will be placed in `UserActivityLogger/bin/Release/UserActivityLogger.<version>.nupkg`.
