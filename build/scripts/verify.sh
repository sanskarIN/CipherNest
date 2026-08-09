#!/usr/bin/env bash
set -euo pipefail

dotnet format CipherNest.slnx --verify-no-changes
dotnet test tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj -c Release
dotnet test tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj -c Release
dotnet test tests/CipherNest.UiTests/CipherNest.UiTests.csproj -c Release
printf 'Core verification completed.\n'
