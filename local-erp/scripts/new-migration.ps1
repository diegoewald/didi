param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]*$')]
    [string]$Name
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet tool restore
    dotnet ef migrations add $Name `
        --project src/LocalErp.Persistence/LocalErp.Persistence.csproj `
        --startup-project src/LocalErp.Persistence/LocalErp.Persistence.csproj `
        --context ErpDbContext `
        --output-dir Migrations
}
finally {
    Pop-Location
}
