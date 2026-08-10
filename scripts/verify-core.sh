#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPO_ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
cd "$REPO_ROOT"

dotnet --info

TEST_PROJECTS="
tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj
tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj
tests/CipherNest.UiTests/CipherNest.UiTests.csproj
"

for project in $TEST_PROJECTS; do
  dotnet restore "$project"
done

for project in $TEST_PROJECTS; do
  dotnet build "$project" -c Release --no-restore
done

for project in $TEST_PROJECTS; do
  dotnet test "$project" -c Release --no-build
done

FORMAT_PROJECTS="
src/CipherNest.Domain/CipherNest.Domain.csproj
src/CipherNest.Application/CipherNest.Application.csproj
src/CipherNest.Infrastructure/CipherNest.Infrastructure.csproj
src/CipherNest.Shared/CipherNest.Shared.csproj
tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj
tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj
tests/CipherNest.UiTests/CipherNest.UiTests.csproj
"

for project in $FORMAT_PROJECTS; do
  dotnet format "$project" --verify-no-changes --no-restore
done
