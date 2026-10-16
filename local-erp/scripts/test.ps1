$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet restore LocalErp.sln
    dotnet build LocalErp.sln -c Release --no-restore
    dotnet test LocalErp.sln -c Release --no-build
}
finally {
    Pop-Location
}
