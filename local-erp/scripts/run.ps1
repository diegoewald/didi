$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet run --project src/LocalErp.Desktop/LocalErp.Desktop.csproj
}
finally {
    Pop-Location
}
