#!/usr/bin/env bash
set -euo pipefail

echo "Restoring dependencies..."
dotnet restore UserActivityLogger.sln

echo "Building solution..."
dotnet build UserActivityLogger.sln -c Release --no-restore

echo "Running unit tests..."
dotnet test tests/UserActivityLogger.UnitTests/UserActivityLogger.UnitTests.csproj -c Release --no-build

echo "Packing library package..."
dotnet pack UserActivityLogger/UserActivityLogger.csproj -c Release --no-build

echo "Checking code format..."
dotnet format style --verify-no-changes --no-restore
dotnet format whitespace --verify-no-changes --no-restore --exclude UserActivityLogger/Constants/ConstPredefinedLogMessage.cs UserActivityLogger.Tests/Controllers/DepartmentsController.cs

if command -v python3 >/dev/null 2>&1; then
    echo "Validating documentation links and files..."
    python3 .github/scripts/check-docs.py
fi

echo "Verification completed successfully."
