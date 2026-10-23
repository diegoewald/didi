[CmdletBinding()]
param(
    [string]$Solution = 'LocalErp.sln',

    [switch]$NoRestore,

    [string]$ReportPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$solutionPath = if ([IO.Path]::IsPathRooted($Solution)) {
    [IO.Path]::GetFullPath($Solution)
}
else {
    [IO.Path]::GetFullPath((Join-Path $projectRoot $Solution))
}

function Get-JsonPropertyValue {
    param(
        [Parameter(Mandatory)]
        [object]$Object,

        [Parameter(Mandatory)]
        [string]$Name
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Get-VulnerabilityFindings {
    param(
        [Parameter(Mandatory)]
        [object]$Report
    )

    $projects = Get-JsonPropertyValue -Object $Report -Name 'projects'
    if ($null -eq $projects) {
        throw 'NuGet vulnerability report is missing the projects collection.'
    }

    $findings = [System.Collections.Generic.List[object]]::new()

    foreach ($project in @($projects)) {
        $projectPathValue = Get-JsonPropertyValue -Object $project -Name 'path'
        $projectPath = if ($null -ne $projectPathValue) { [string]$projectPathValue } else { '<unknown-project>' }

        $frameworks = Get-JsonPropertyValue -Object $project -Name 'frameworks'
        if ($null -eq $frameworks) {
            continue
        }

        foreach ($framework in @($frameworks)) {
            $frameworkValue = Get-JsonPropertyValue -Object $framework -Name 'framework'
            $frameworkName = if ($null -ne $frameworkValue) { [string]$frameworkValue } else { '<unknown-framework>' }

            foreach ($group in @(
                @{ Name = 'direct'; Property = 'topLevelPackages' },
                @{ Name = 'transitive'; Property = 'transitivePackages' }
            )) {
                $packages = Get-JsonPropertyValue -Object $framework -Name $group.Property
                if ($null -eq $packages) {
                    continue
                }

                foreach ($package in @($packages)) {
                    $vulnerabilities = Get-JsonPropertyValue -Object $package -Name 'vulnerabilities'
                    if ($null -eq $vulnerabilities) {
                        continue
                    }

                    $packageId = Get-JsonPropertyValue -Object $package -Name 'id'
                    $resolvedVersion = Get-JsonPropertyValue -Object $package -Name 'resolvedVersion'

                    foreach ($vulnerability in @($vulnerabilities)) {
                        $severity = Get-JsonPropertyValue -Object $vulnerability -Name 'severity'
                        $advisoryUrl = Get-JsonPropertyValue -Object $vulnerability -Name 'advisoryurl'

                        $findings.Add([pscustomobject]@{
                            Project = $projectPath
                            Framework = $frameworkName
                            Dependency = $group.Name
                            Package = if ($null -ne $packageId) { [string]$packageId } else { '<unknown-package>' }
                            Version = if ($null -ne $resolvedVersion) { [string]$resolvedVersion } else { '<unknown-version>' }
                            Severity = if ($null -ne $severity) { [string]$severity } else { '<unknown-severity>' }
                            AdvisoryUrl = if ($null -ne $advisoryUrl) { [string]$advisoryUrl } else { '<unknown-advisory>' }
                        })
                    }
                }
            }
        }
    }

    return $findings
}

function Read-VulnerabilityReport {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (-not (Test-Path $Path -PathType Leaf)) {
        throw "Vulnerability report not found at '$Path'."
    }

    $raw = Get-Content $Path -Raw
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw "Vulnerability report '$Path' is empty."
    }

    try {
        return $raw | ConvertFrom-Json -Depth 100
    }
    catch {
        throw "Vulnerability report '$Path' is not valid JSON: $($_.Exception.Message)"
    }
}

if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
    $report = Read-VulnerabilityReport -Path ([IO.Path]::GetFullPath($ReportPath))
}
else {
    if (-not (Test-Path $solutionPath -PathType Leaf)) {
        throw "Solution not found at '$solutionPath'."
    }

    Push-Location $projectRoot
    try {
        if (-not $NoRestore) {
            & dotnet restore $solutionPath
            if ($LASTEXITCODE -ne 0) {
                throw "dotnet restore failed with exit code $LASTEXITCODE."
            }
        }

        $arguments = @(
            'package', 'list',
            '--project', $solutionPath,
            '--vulnerable',
            '--include-transitive',
            '--format', 'json',
            '--output-version', '1',
            '--no-restore'
        )

        $json = & dotnet @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet package list vulnerability scan failed with exit code $LASTEXITCODE."
        }

        $rawReport = $json -join [Environment]::NewLine
        if ([string]::IsNullOrWhiteSpace($rawReport)) {
            throw 'NuGet vulnerability scan returned an empty report.'
        }

        try {
            $report = $rawReport | ConvertFrom-Json -Depth 100
        }
        catch {
            throw "NuGet vulnerability scan returned invalid JSON: $($_.Exception.Message)"
        }
    }
    finally {
        Pop-Location
    }
}

$findings = @(Get-VulnerabilityFindings -Report $report)
if ($findings.Count -gt 0) {
    Write-Host "NuGet vulnerability audit found $($findings.Count) advisory finding(s):"
    foreach ($finding in $findings | Sort-Object Project, Framework, Dependency, Package, Version, Severity, AdvisoryUrl) {
        Write-Host ("- [{0}] {1} {2} ({3}, {4}, {5}) {6}" -f `
            $finding.Severity,
            $finding.Package,
            $finding.Version,
            $finding.Dependency,
            $finding.Framework,
            $finding.Project,
            $finding.AdvisoryUrl)
    }

    throw 'Known vulnerable NuGet dependencies are not allowed in the validated baseline.'
}

Write-Host 'NuGet vulnerability audit passed: no known vulnerable direct or transitive packages were reported.'
