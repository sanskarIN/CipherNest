Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
    throw 'scripts/verify-windows.ps1 must run on Windows.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    $project = 'src/CipherNest.App/CipherNest.App.csproj'
    $framework = 'net10.0-windows10.0.19041.0'
    $rid = 'win-x64'

    dotnet workload install maui --skip-manifest-update
    if ($LASTEXITCODE -ne 0) { throw 'MAUI workload install failed.' }

    dotnet build $project -c Release -p:CipherNestTargetFrameworks=$framework -f $framework -r $rid -p:WindowsPackageType=None
    if ($LASTEXITCODE -ne 0) { throw 'Default Windows app build failed.' }

    dotnet build $project -c Release -p:CipherNestTargetFrameworks=$framework -f $framework -r $rid --no-restore -p:WindowsPackageType=None -p:CipherNestEnableFundingLink=false
    if ($LASTEXITCODE -ne 0) { throw 'Funding-disabled Windows app build failed.' }
}
finally {
    Pop-Location
}
