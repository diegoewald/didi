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

function Get-VulnerabilityFindings {
    param(
        [Parameter(Mandatory)]
        [object]$Report
    )

    if ($null -eq $Report.projects) {
        throw 'NuGet vulnerability report is missing the projects collection.'
    }

    $findings = [System.Collections.Generic.List[object]]::new()

    foreach ($project in @($Report.projects)) {
        $projectPath = if ($null -ne $project.path) { [string]$project.path } else { '<unknown-project>' }

        foreach ($framework in @($project.frameworks)) {
            $frameworkName = if ($null -ne $framework.framework) { [string]$framework.framework } else { '<unknown-framework>' }

            foreach ($group in @(
                @{ Name = 'direct'; Property = 'topLevelPackages' },
                @{ Name = 'transitive'; Property = 'transitivePackages' }
            )) {
                $property = $framework.PSObject.Properties[$group.Property]
                if ($null -eq $property -or $null -eq $property.Value) {
                    continue
                }

                foreach ($package in @($property.Value)) {
                    $vulnerabilitiesProperty = $package.PSObject.Properties['vulnerabilities']
                    if ($null -eq $vulnerabilitiesProperty -or $null -eq $vulnerabilitiesProperty.Value) {
                        continue
                    }

                    foreach ($vulnerability in @($vulnerabilitiesProperty.Value)) {
                        $findings.Add([pscustomobject]@{
                            Project = $projectPath
                            Framework = $frameworkName
                            Dependency = $group.Name
                            Package = [string]$package.id
                            Version = [string]$package.resolvedVersion
                            Severity = [string]$vulnerability.severity
                            AdvisoryUrl = [string]$vulnerability.advisoryurl
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

        if ([string]::IsNullOrWhiteSpace(($json -join [Environment]::NewLine))) {
            throw 'NuGet vulnerability scan returned an empty report.'
        }

        try {
            $report = ($json -join [Environment]::NewLine) | ConvertFrom-Json -Depth 100
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
