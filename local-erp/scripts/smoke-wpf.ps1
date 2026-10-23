[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$smokeRoot = Join-Path $env:TEMP ("LocalErp\WpfSmoke\" + [Guid]::NewGuid().ToString('N'))
$dbPath = Join-Path $smokeRoot 'Data\localerp.db'
$appExe = Join-Path $projectRoot "src\LocalErp.Desktop\bin\$Configuration\net10.0-windows\LocalErp.Desktop.exe"
$previousRoot = $env:LOCALERP_DATA_ROOT
$process = $null

function Wait-Window {
    param([int]$ProcessId, [string]$Title, [int]$TimeoutSeconds = 30)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
        $ProcessId)

    while ((Get-Date) -lt $deadline) {
        if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) {
            throw "Local ERP exited before window '$Title' became available."
        }

        $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            $condition)
        foreach ($candidate in $windows) {
            if ($candidate.Current.Name -eq $Title) {
                return $candidate
            }
        }
        Start-Sleep -Milliseconds 200
    }

    throw "Timed out waiting for window '$Title'."
}

function Find-ByAutomationId {
    param($Root, [string]$AutomationId)
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)
    $element = $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if ($null -eq $element) { throw "Automation element '$AutomationId' was not found." }
    return $element
}

function Find-ByName {
    param($Root, [string]$Name)
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name)
    $element = $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if ($null -eq $element) { throw "Automation element named '$Name' was not found." }
    return $element
}

function Set-Value {
    param($Element, [string]$Value)
    $pattern = $Element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    ([System.Windows.Automation.ValuePattern]$pattern).SetValue($Value)
}

function Invoke-Element {
    param($Element)
    $pattern = $Element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    ([System.Windows.Automation.InvokePattern]$pattern).Invoke()
}

function Select-Tab {
    param($Root, [string]$Name)
    $tab = Find-ByName $Root $Name
    $pattern = $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    ([System.Windows.Automation.SelectionItemPattern]$pattern).Select()
    Start-Sleep -Milliseconds 700
}

try {
    Remove-Item $smokeRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path (Split-Path $dbPath -Parent) -Force | Out-Null
    $env:LOCALERP_DATA_ROOT = $smokeRoot

    $connection = "Data Source=$dbPath;Mode=ReadWriteCreate;Foreign Keys=True;Default Timeout=15"
    & dotnet ef database update --project (Join-Path $projectRoot 'src\LocalErp.Persistence\LocalErp.Persistence.csproj') --startup-project (Join-Path $projectRoot 'src\LocalErp.Persistence\LocalErp.Persistence.csproj') --context ErpDbContext --connection $connection --configuration $Configuration --no-build
    if ($LASTEXITCODE -ne 0) { throw "Failed to prepare the isolated smoke database." }
    if (-not (Test-Path $appExe)) { throw "Desktop executable not found at '$appExe'. Build the solution first." }

    $process = Start-Process -FilePath $appExe -PassThru
    $bootstrap = Wait-Window $process.Id 'Configuração inicial — Local ERP'

    Set-Value (Find-ByAutomationId $bootstrap 'UsernameTextBox') 'ci-smoke-admin'
    Set-Value (Find-ByAutomationId $bootstrap 'PasswordInput') 'SmokePassword-2026!'
    Set-Value (Find-ByAutomationId $bootstrap 'ConfirmPasswordInput') 'SmokePassword-2026!'
    Invoke-Element (Find-ByAutomationId $bootstrap 'CreateButton')

    $login = Wait-Window $process.Id 'Entrar — Local ERP'
    Set-Value (Find-ByAutomationId $login 'UsernameTextBox') 'ci-smoke-admin'
    Set-Value (Find-ByAutomationId $login 'PasswordInput') 'SmokePassword-2026!'
    Invoke-Element (Find-ByAutomationId $login 'SignInButton')

    $main = Wait-Window $process.Id 'Local ERP'
    Invoke-Element (Find-ByName $main 'Compras')
    Start-Sleep -Milliseconds 800
    Select-Tab $main 'Solicitações'
    Select-Tab $main 'Cotações'
    Select-Tab $main 'Pedidos'

    Invoke-Element (Find-ByName $main 'Sair')
    $loginAfterLogout = Wait-Window $process.Id 'Entrar — Local ERP'
    Invoke-Element (Find-ByAutomationId $loginAfterLogout 'CancelButton')

    if (-not $process.WaitForExit(10000)) {
        throw 'Local ERP did not exit after the logout/login cancel smoke path.'
    }
    if ($process.ExitCode -ne 0) { throw "Local ERP exited with code $($process.ExitCode)." }

    $logFile = Get-ChildItem (Join-Path $smokeRoot 'Logs\localerp-*.log') -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if ($null -eq $logFile) { throw 'Smoke log file was not created.' }
    $log = Get-Content $logFile.FullName -Raw
    foreach ($route in @('AdministratorBootstrapRequired', 'SignInRequired', 'Main')) {
        if ($log -notmatch [Regex]::Escape("Startup access route resolved as $route")) {
            throw "Expected startup route '$route' was not recorded."
        }
    }
    if ($log -match '\[FTL\]') { throw 'Fatal log entry detected during WPF smoke.' }

    Write-Host 'WPF smoke passed: bootstrap -> login -> main -> Compras tabs -> logout.'
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
    $env:LOCALERP_DATA_ROOT = $previousRoot
    Remove-Item $smokeRoot -Recurse -Force -ErrorAction SilentlyContinue
}
