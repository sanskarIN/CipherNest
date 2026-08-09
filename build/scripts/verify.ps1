$ErrorActionPreference = 'Stop'
Write-Host 'Formatting check'
dotnet format CipherNest.slnx --verify-no-changes
Write-Host 'Unit tests'
dotnet test tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj -c Release
Write-Host 'Integration tests'
dotnet test tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj -c Release
Write-Host 'UI structure tests'
dotnet test tests/CipherNest.UiTests/CipherNest.UiTests.csproj -c Release
Write-Host 'Core verification completed.'
