[CmdletBinding()]
param(
    [string]$DataRoot,
    [string]$MigratorPath = (Join-Path $PSScriptRoot 'LocalErp.DatabaseMigrator.exe')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($DataRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($env:LOCALERP_DATA_ROOT)) {
        $DataRoot = $env:LOCALERP_DATA_ROOT
    }
    else {
        $DataRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'LocalErp'
    }
}

$root = [IO.Path]::GetFullPath($DataRoot)
$dataDirectory = Join-Path $root 'Data'
$databasePath = Join-Path $dataDirectory 'localerp.db'
$migrator = [IO.Path]::GetFullPath($MigratorPath)

if (-not (Test-Path $migrator -PathType Leaf)) {
    throw "Migration bundle not found at '$migrator'."
}

if (Test-Path $databasePath) {
    throw "Refusing to modify existing database '$databasePath'. This helper is only for a fresh first-run database. Existing databases require the reviewed backup/update flow."
}

New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null

$escapedPath = $databasePath.Replace('"', '""')
$connection = 'Data Source="' + $escapedPath + '";Mode=ReadWriteCreate;Foreign Keys=True;Default Timeout=15'
$previousEnvironment = $env:DOTNET_ENVIRONMENT

try {
    $env:DOTNET_ENVIRONMENT = 'Production'
    & $migrator --connection $connection --no-color
    if ($LASTEXITCODE -ne 0) {
        throw "Migration bundle exited with code $LASTEXITCODE."
    }

    if (-not (Test-Path $databasePath -PathType Leaf)) {
        throw "Migration bundle finished without creating '$databasePath'."
    }

    Write-Host "Fresh Local ERP database initialized at '$databasePath'."
}
catch {
    Remove-Item $databasePath -Force -ErrorAction SilentlyContinue
    Remove-Item "$databasePath-wal" -Force -ErrorAction SilentlyContinue
    Remove-Item "$databasePath-shm" -Force -ErrorAction SilentlyContinue
    throw
}
finally {
    $env:DOTNET_ENVIRONMENT = $previousEnvironment
}
