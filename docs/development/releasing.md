# Release and publishing guide

This guide covers semantic versioning, release preparation, automated NuGet publishing via GitHub Actions, and unlisting procedures for UserActivityLogger maintainers.

## Versioning scheme

UserActivityLogger follows Semantic Versioning 2.0.0 (MAJOR.MINOR.PATCH):
- MAJOR: Incompatible public API modifications or breaking entity schema alterations.
- MINOR: Backward-compatible functionality additions or new options.
- PATCH: Backward-compatible bug fixes, optimizations, or documentation corrections.

The single source of truth for the package version is the `<Version>` tag in `UserActivityLogger/UserActivityLogger.csproj`.

## Release preparation checklist

Complete these steps before publishing a release:

1. Ensure the `master` branch is passing all continuous integration checks.
2. Update `CHANGELOG.md`:
   - Move entries from the `[Unreleased]` section into a new section titled with the new version and release date (e.g., `## [1.0.11] - 2026-11-15`).
3. Update the package version in `UserActivityLogger/UserActivityLogger.csproj`:
   ```xml
   <Version>1.0.11</Version>
   ```
4. Run local verification:
   ```bash
   ./eng/verify.sh
   ```
5. Commit the version bump and changelog update:
   ```bash
   git commit -am "chore(release): bump version to 1.0.11"
   git push upstream master
   ```

## Automated publishing workflow

Publishing is automated via `.github/workflows/publish.yml`. The workflow triggers when an annotated git tag matching `v*.*.*` is pushed to the upstream repository:

```bash
git tag -a v1.0.11 -m "Release v1.0.11"
git push upstream v1.0.11
```

The workflow performs the following sequence:
1. Validates repository ownership: The job runs only on the upstream repository (`Asma-Elhadiedy/UserActivityLogger`), never on forks.
2. Validates tag matching: Checks that the tag name (without `v`, e.g. `1.0.11`) exactly matches the `<Version>` element in `UserActivityLogger/UserActivityLogger.csproj`. If the version in the project file does not match the tag, the workflow terminates immediately.
3. Builds and packs: Restores dependencies and builds `UserActivityLogger.sln` in Release configuration across `net8.0`, `net9.0`, and `net10.0`, then generates `.nupkg` and `.snupkg` artifacts.
4. Pushes to NuGet: Executes `dotnet nuget push` to `https://api.nuget.org/v3/index.json` using the repository secret `NUGET_API_KEY` with `--skip-duplicate`.
5. Creates GitHub release: Creates a GitHub Release associated with the git tag and attaches the compiled package artifacts.

## Workflow failure behavior

- Missing secret: If `NUGET_API_KEY` is not configured in repository secrets, the workflow terminates with an error message and does not attempt publishing.
- Version mismatch: If the git tag does not match the `.csproj` version, the job fails at the validation step before running the build.
- Duplicate push: If the version already exists on NuGet.org, the `--skip-duplicate` flag prevents the job from failing, but does not overwrite the remote package.
- Release creation failure: If the NuGet push fails, subsequent steps (including the GitHub Release creation) do not execute.

## Rollback and package unlisting

NuGet.org does not permit deleting packages permanently once published, preserving reproducibility for consuming builds. If a published release contains a critical bug or vulnerability:

1. Unlist the package via the NuGet.org web portal:
   - Navigate to `https://www.nuget.org/packages/UserActivityLogger/<version>/Manage`.
   - Under the Listing tab, uncheck "List in search results" and save.

2. Alternatively, unlist via the .NET CLI:
   ```bash
   dotnet nuget delete UserActivityLogger 1.0.11 \
     --api-key "<NUGET_API_KEY>" \
     --non-interactive \
     --source https://api.nuget.org/v3/index.json
   ```

Unlisting prevents the package from appearing in search results or package manager suggestions, but allows existing projects with pinned versions to continue restoring.

3. Publish a patched release:
   - Fix the issue on a branch, increment the patch version (e.g., `1.0.12`), and push a new tag.

## Manual fallback publishing

If GitHub Actions is unavailable, maintainers can publish manually:

1. Pack the library locally:
   ```bash
   dotnet pack UserActivityLogger/UserActivityLogger.csproj -c Release --output ./artifacts
   ```

2. Publish with your personal NuGet API key:
   ```bash
   dotnet nuget push "./artifacts/UserActivityLogger.1.0.11.nupkg" \
     --api-key "<NUGET_API_KEY>" \
     --source https://api.nuget.org/v3/index.json \
     --skip-duplicate
   ```
