[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidatePattern('^win-(x64|arm64)$')]
    [string]$Runtime = 'win-x64',

    [string]$Version,

    [string]$OutputDirectory = 'artifacts\release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$desktopProject = Join-Path $projectRoot 'src\LocalErp.Desktop\LocalErp.Desktop.csproj'
$persistenceProject = Join-Path $projectRoot 'src\LocalErp.Persistence\LocalErp.Persistence.csproj'
$propsPath = Join-Path $projectRoot 'Directory.Build.props'

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$props = Get-Content $propsPath -Raw
    $versionCandidates = @(
        $props.Project.PropertyGroup |
            ForEach-Object { $_.VersionPrefix } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
    )
    if ($versionCandidates.Count -eq 0) {
        throw 'VersionPrefix was not found in Directory.Build.props.'
    }
    $Version = [string]$versionCandidates[0]
}

if ($Version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a supported semantic version for packaging."
}

$outputRoot = if ([IO.Path]::IsPathRooted($OutputDirectory)) {
    [IO.Path]::GetFullPath($OutputDirectory)
}
else {
    [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))
}

$packageName = "LocalErp-$Version-$Runtime"
$packageDirectory = Join-Path $outputRoot $packageName
$appDirectory = Join-Path $packageDirectory 'app'
$databaseDirectory = Join-Path $packageDirectory 'database'
$zipPath = Join-Path $outputRoot "$packageName.zip"
$zipHashPath = "$zipPath.sha256"
$migratorPath = Join-Path $databaseDirectory 'LocalErp.DatabaseMigrator.exe'
$previousEnvironment = $env:DOTNET_ENVIRONMENT

Remove-Item $packageDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Remove-Item $zipHashPath -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $appDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $databaseDirectory -Force | Out-Null

Push-Location $projectRoot
try {
    $env:DOTNET_ENVIRONMENT = 'Production'

    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed.' }

    & dotnet publish $desktopProject `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained true `
        --output $appDirectory `
        "-p:Version=$Version"
    if ($LASTEXITCODE -ne 0) { throw 'Desktop self-contained publish failed.' }

    & dotnet ef migrations bundle `
        --self-contained `
        --target-runtime $Runtime `
        --force `
        --output $migratorPath `
        --project $persistenceProject `
        --startup-project $persistenceProject `
        --context ErpDbContext `
        --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'EF Core migration bundle generation failed.' }

    Copy-Item (Join-Path $PSScriptRoot 'initialize-fresh-database.ps1') `
        (Join-Path $databaseDirectory 'Initialize-FreshDatabase.ps1') -Force
    Copy-Item (Join-Path $projectRoot 'docs\PACKAGING.md') `
        (Join-Path $packageDirectory 'README.md') -Force

    $commit = if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_SHA)) {
        $env:GITHUB_SHA
    }
    else {
        $resolvedCommit = (& git rev-parse HEAD 2>$null)
        if ($LASTEXITCODE -eq 0) { [string]$resolvedCommit } else { 'unknown' }
    }

    $releaseInfo = [ordered]@{
        product = 'Local ERP'
        version = $Version
        runtime = $Runtime
        configuration = $Configuration
        selfContained = $true
        commit = $commit.Trim()
        createdAtUtc = [DateTime]::UtcNow.ToString('O')
        packageKind = 'portable-qa'
        productionInstaller = $false
    }
    $releaseInfo | ConvertTo-Json | Set-Content (Join-Path $packageDirectory 'release-info.json') -Encoding utf8

    $appExe = Join-Path $appDirectory 'LocalErp.Desktop.exe'
    if (-not (Test-Path $appExe -PathType Leaf)) {
        throw "Published executable not found at '$appExe'."
    }
    if (-not (Test-Path $migratorPath -PathType Leaf)) {
        throw "Migration bundle not found at '$migratorPath'."
    }

    $hashLines = Get-ChildItem $packageDirectory -File -Recurse |
        Where-Object { $_.Name -ne 'SHA256SUMS.txt' } |
        Sort-Object FullName |
        ForEach-Object {
            $relativePath = [IO.Path]::GetRelativePath($packageDirectory, $_.FullName).Replace('\', '/')
            $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            "$hash  $relativePath"
        }
    $hashLines | Set-Content (Join-Path $packageDirectory 'SHA256SUMS.txt') -Encoding ascii

    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
    Compress-Archive -Path $packageDirectory -DestinationPath $zipPath -CompressionLevel Optimal

    $zipHash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$zipHash  $([IO.Path]::GetFileName($zipPath))" | Set-Content $zipHashPath -Encoding ascii -NoNewline

    Write-Host "Windows package created: $zipPath"
    Write-Host "Package directory: $packageDirectory"
    Write-Host "SHA256: $zipHash"
}
finally {
    $env:DOTNET_ENVIRONMENT = $previousEnvironment
    Pop-Location
}
