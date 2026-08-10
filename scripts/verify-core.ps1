Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    dotnet --info

    $testProjects = @(
        'tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj',
        'tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj',
        'tests/CipherNest.UiTests/CipherNest.UiTests.csproj'
    )

    foreach ($project in $testProjects) {
        dotnet restore $project
        if ($LASTEXITCODE -ne 0) { throw "Restore failed: $project" }
    }

    foreach ($project in $testProjects) {
        dotnet build $project -c Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
    }

    foreach ($project in $testProjects) {
        dotnet test $project -c Release --no-build
        if ($LASTEXITCODE -ne 0) { throw "Tests failed: $project" }
    }

    $formatProjects = @(
        'src/CipherNest.Domain/CipherNest.Domain.csproj',
        'src/CipherNest.Application/CipherNest.Application.csproj',
        'src/CipherNest.Infrastructure/CipherNest.Infrastructure.csproj',
        'src/CipherNest.Shared/CipherNest.Shared.csproj',
        'tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj',
        'tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj',
        'tests/CipherNest.UiTests/CipherNest.UiTests.csproj'
    )

    foreach ($project in $formatProjects) {
        dotnet format $project --verify-no-changes --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Formatting verification failed: $project" }
    }
}
finally {
    Pop-Location
}
