[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$NoBuild,

    [switch]$NoRestore,

    [switch]$NoToolRestore,

    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$solutionPath = Join-Path $projectRoot 'LocalErp.sln'
$settingsPath = Join-Path $projectRoot 'coverage.runsettings'

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot 'artifacts\coverage'
}
elseif (-not [IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot $OutputDirectory
}

$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$rawRoot = Join-Path $outputRoot 'raw'
$coberturaPath = Join-Path $outputRoot 'coverage.cobertura.xml'
$summaryPath = Join-Path $outputRoot 'coverage-summary.json'

if (-not (Test-Path $solutionPath -PathType Leaf)) {
    throw "Solution not found at '$solutionPath'."
}

if (-not (Test-Path $settingsPath -PathType Leaf)) {
    throw "Coverage settings not found at '$settingsPath'."
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FailureMessage Exit code: $LASTEXITCODE."
    }
}

function Get-RequiredDoubleAttribute {
    param(
        [Parameter(Mandatory)]
        [System.Xml.XmlElement]$Element,

        [Parameter(Mandatory)]
        [string]$Name
    )

    $value = $Element.GetAttribute($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Coverage report is missing required '$Name' attribute."
    }

    $parsed = 0.0
    if (-not [double]::TryParse(
        $value,
        [Globalization.NumberStyles]::Float,
        [Globalization.CultureInfo]::InvariantCulture,
        [ref]$parsed)) {
        throw "Coverage report contains invalid '$Name' value '$value'."
    }

    return $parsed
}

function Get-OptionalInt64Attribute {
    param(
        [Parameter(Mandatory)]
        [System.Xml.XmlElement]$Element,

        [Parameter(Mandatory)]
        [string]$Name
    )

    $value = $Element.GetAttribute($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $null
    }

    $parsed = [long]0
    if (-not [long]::TryParse(
        $value,
        [Globalization.NumberStyles]::Integer,
        [Globalization.CultureInfo]::InvariantCulture,
        [ref]$parsed)) {
        throw "Coverage report contains invalid '$Name' value '$value'."
    }

    return $parsed
}

Push-Location $projectRoot
try {
    if (Test-Path $outputRoot) {
        Remove-Item $outputRoot -Recurse -Force
    }

    New-Item $rawRoot -ItemType Directory -Force | Out-Null

    if (-not $NoToolRestore) {
        Invoke-DotNet -Arguments @('tool', 'restore') -FailureMessage 'dotnet tool restore failed.'
    }

    if (-not $NoRestore) {
        Invoke-DotNet -Arguments @('restore', $solutionPath) -FailureMessage 'dotnet restore failed.'
    }

    $testArguments = @(
        'test', $solutionPath,
        '--configuration', $Configuration,
        '--collect', 'Code Coverage',
        '--settings', $settingsPath,
        '--results-directory', $rawRoot
    )

    if ($NoBuild) {
        $testArguments += '--no-build'
    }

    if ($NoRestore) {
        $testArguments += '--no-restore'
    }

    Invoke-DotNet -Arguments $testArguments -FailureMessage 'Coverage test run failed.'

    $coverageFiles = @(
        Get-ChildItem $rawRoot -Recurse -File -Filter '*.coverage' |
            Sort-Object FullName
    )

    if ($coverageFiles.Count -eq 0) {
        throw "Coverage test run produced no .coverage files under '$rawRoot'."
    }

    $mergeArguments = @('tool', 'run', 'dotnet-coverage', '--', 'merge')
    $mergeArguments += @($coverageFiles | ForEach-Object FullName)
    $mergeArguments += @('-o', $coberturaPath, '-f', 'cobertura', '--nologo')
    Invoke-DotNet -Arguments $mergeArguments -FailureMessage 'dotnet-coverage merge failed.'

    if (-not (Test-Path $coberturaPath -PathType Leaf)) {
        throw "Merged Cobertura report was not created at '$coberturaPath'."
    }

    [xml]$coverageDocument = Get-Content $coberturaPath -Raw
    $coverageElement = $coverageDocument.DocumentElement
    if ($null -eq $coverageElement -or $coverageElement.Name -ne 'coverage') {
        throw 'Merged coverage report does not contain a Cobertura coverage root element.'
    }

    $lineRate = Get-RequiredDoubleAttribute -Element $coverageElement -Name 'line-rate'
    $branchRate = Get-RequiredDoubleAttribute -Element $coverageElement -Name 'branch-rate'
    $linesCovered = Get-OptionalInt64Attribute -Element $coverageElement -Name 'lines-covered'
    $linesValid = Get-OptionalInt64Attribute -Element $coverageElement -Name 'lines-valid'
    $branchesCovered = Get-OptionalInt64Attribute -Element $coverageElement -Name 'branches-covered'
    $branchesValid = Get-OptionalInt64Attribute -Element $coverageElement -Name 'branches-valid'

    $packageSummaries = @(
        foreach ($packageNode in @($coverageDocument.SelectNodes('/coverage/packages/package'))) {
            $packageElement = [System.Xml.XmlElement]$packageNode
            [pscustomobject][ordered]@{
                name = $packageElement.GetAttribute('name')
                lineRate = Get-RequiredDoubleAttribute -Element $packageElement -Name 'line-rate'
                branchRate = Get-RequiredDoubleAttribute -Element $packageElement -Name 'branch-rate'
            }
        }
    ) | Sort-Object name

    if ($packageSummaries.Count -eq 0) {
        throw 'Merged coverage report contains no covered LocalErp packages.'
    }

    $summary = [ordered]@{
        schemaVersion = 1
        generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        configuration = $Configuration
        rawReportCount = $coverageFiles.Count
        thresholdEnforced = $false
        policy = 'Baseline observability only; collection validity is gated, percentages are not.'
        overall = [ordered]@{
            lineRate = [Math]::Round($lineRate, 6)
            linePercent = [Math]::Round($lineRate * 100, 2)
            branchRate = [Math]::Round($branchRate, 6)
            branchPercent = [Math]::Round($branchRate * 100, 2)
            linesCovered = $linesCovered
            linesValid = $linesValid
            branchesCovered = $branchesCovered
            branchesValid = $branchesValid
        }
        packages = @($packageSummaries)
    }

    $summary | ConvertTo-Json -Depth 8 | Set-Content $summaryPath -Encoding utf8

    $linePercentText = ('{0:F2}%' -f ($lineRate * 100))
    $branchPercentText = ('{0:F2}%' -f ($branchRate * 100))

    Write-Host "Coverage baseline generated successfully."
    Write-Host "Line coverage:   $linePercentText"
    Write-Host "Branch coverage: $branchPercentText"
    Write-Host "Packages:        $($packageSummaries.Count)"
    Write-Host "Cobertura:       $coberturaPath"
    Write-Host "Summary:         $summaryPath"
    Write-Host 'Policy: no percentage threshold is enforced; invalid or empty collection still fails.'

    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
        @(
            '## Local ERP code coverage baseline',
            '',
            "- Line coverage: **$linePercentText**",
            "- Branch coverage: **$branchPercentText**",
            "- Covered packages: **$($packageSummaries.Count)**",
            "- Raw reports merged: **$($coverageFiles.Count)**",
            '- Policy: baseline/observability only; no arbitrary percentage threshold is enforced.'
        ) | Add-Content $env:GITHUB_STEP_SUMMARY -Encoding utf8
    }
}
finally {
    Pop-Location
}
