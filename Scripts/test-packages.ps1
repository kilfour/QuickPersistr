param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"

function Invoke-DotNet {
    param([string[]] $Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$smokeRoot = Join-Path $repositoryRoot "artifacts/package-smoke"
$packageSource = Join-Path $smokeRoot "packages"
$consumerProject = Join-Path $PSScriptRoot "PackageSmoke/PackageSmoke.csproj"
$nugetConfig = Join-Path $PSScriptRoot "PackageSmoke/NuGet.Config"
$packageVersion = "0.0.0-smoke"

if (Test-Path -LiteralPath $smokeRoot) {
    Remove-Item -LiteralPath $smokeRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $packageSource | Out-Null

$projects = @(
    "QuickPersistr/QuickPersistr.csproj",
    "QuickPersistr.EntityFrameworkCore/QuickPersistr.EntityFrameworkCore.csproj",
    "QuickPersistr.EntityFrameworkCore.Sqlite/QuickPersistr.EntityFrameworkCore.Sqlite.csproj",
    "QuickPersistr.EntityFrameworkCore.PostgreSql/QuickPersistr.EntityFrameworkCore.PostgreSql.csproj"
)

foreach ($project in $projects) {
    Invoke-DotNet -Arguments @(
        "pack",
        (Join-Path $repositoryRoot $project),
        "--configuration", $Configuration,
        "--output", $packageSource,
        "-p:PackageVersion=$packageVersion"
    )
}

Invoke-DotNet -Arguments @(
    "restore", $consumerProject,
    "--force",
    "--configfile", $nugetConfig
)
Invoke-DotNet -Arguments @(
    "build", $consumerProject,
    "--configuration", $Configuration,
    "--no-restore"
)
Invoke-DotNet -Arguments @(
    "run", "--project", $consumerProject,
    "--configuration", $Configuration,
    "--no-build",
    "--no-restore"
)

Write-Host "Package smoke test passed using $packageSource"
