# Release & Publishing Guide

This document outlines the release process, versioning conventions, and NuGet publishing instructions for `UserActivityLogger` maintainers.

---

## 1. Versioning Scheme

`UserActivityLogger` follows [Semantic Versioning 2.0.0](https://semver.org/):

$$\text{MAJOR}.\text{MINOR}.\text{PATCH}$$

- **MAJOR**: Incompatible public API changes, removed types, or breaking schema adjustments.
- **MINOR**: New backward-compatible features, extension points, or options.
- **PATCH**: Backward-compatible bug fixes, performance improvements, or documentation updates.

---

## 2. Release Preparation Checklist

Before cutting a new release:
1. Ensure all CI checks pass on the `master` branch.
2. Update `CHANGELOG.md`:
   - Move relevant items from `[Unreleased]` into a new section titled with the new version and release date (e.g. `## [1.0.11] - 2026-11-15`).
3. Update the package version in `UserActivityLogger/UserActivityLogger.csproj`:
   ```xml
   <Version>1.0.11</Version>
   ```
4. Verify locally using `./eng/verify.sh`.
5. Commit with a conventional release message:
   ```bash
   git commit -am "chore(release): bump version to 1.0.11"
   ```

---

## 3. Cutting a Release via Git Tag

Pushing a version tag triggers the automated GitHub Actions NuGet publish workflow (`.github/workflows/publish.yml`):

```bash
# Create annotated git tag
git tag -a v1.0.11 -m "Release v1.0.11"

# Push tag to upstream
git push upstream v1.0.11
```

The workflow:
1. Checks out the tagged commit.
2. Restores and builds the library with multi-targeting.
3. Generates the release NuGet package and symbol package.
4. Automatically publishes to [NuGet.org](https://www.nuget.org/) using the repository secret `NUGET_API_KEY`.
5. Creates a GitHub Release with the corresponding release notes.

---

## 4. Manual NuGet Publishing (Fallback)

If automated publishing is temporarily unavailable:

1. Build and pack locally:
   ```bash
   dotnet pack UserActivityLogger/UserActivityLogger.csproj -c Release
   ```
2. Push the package using your API key:
   ```bash
   dotnet nuget push UserActivityLogger/bin/Release/UserActivityLogger.1.0.11.nupkg \
     --api-key "<YOUR_NUGET_API_KEY>" \
     --source https://api.nuget.org/v3/index.json
   ```
