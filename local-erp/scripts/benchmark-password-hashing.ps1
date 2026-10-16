param(
    [int]$Samples = 20,
    [int]$Warmup = 2,
    [int]$Iterations = 220000
)

$ErrorActionPreference = 'Stop'

dotnet run --configuration Release --project tools/LocalErp.PasswordHashBenchmark/LocalErp.PasswordHashBenchmark.csproj -- `
    --samples $Samples `
    --warmup $Warmup `
    --iterations $Iterations
