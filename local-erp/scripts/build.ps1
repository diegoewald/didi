$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet restore LocalErp.sln
    dotnet build LocalErp.sln -c Release --no-restore
}
finally {
    Pop-Location
}
