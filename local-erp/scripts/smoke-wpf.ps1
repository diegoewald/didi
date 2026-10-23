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

function Find-ByNameAndControlType {
    param($Root, [string]$Name, $ControlType)
    $condition = [System.Windows.Automation.AndCondition]::new(
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::NameProperty,
            $Name),
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            $ControlType))
    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Find-ButtonByLabel {
    param($Root, [string]$Name)

    $button = Find-ByNameAndControlType $Root $Name ([System.Windows.Automation.ControlType]::Button)
    if ($null -ne $button) { return $button }

    $nameCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name)
    $labels = $Root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $nameCondition)
    foreach ($label in $labels) {
        $current = $label
        while ($null -ne $current) {
            if ($current.Current.ControlType -eq [System.Windows.Automation.ControlType]::Button) {
                return $current
            }
            if ($current -eq $Root) { break }
            $current = [System.Windows.Automation.TreeWalker]::RawViewWalker.GetParent($current)
        }
    }

    throw "Button labelled '$Name' was not found."
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

function Invoke-Button {
    param($Root, [string]$Name)
    Invoke-Element (Find-ButtonByLabel $Root $Name)
}

function Select-Tab {
    param($Root, [string]$Name)
    $tab = Find-ByNameAndControlType $Root $Name ([System.Windows.Automation.ControlType]::TabItem)
    if ($null -eq $tab) { throw "Tab '$Name' was not found." }
    $pattern = $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    ([System.Windows.Automation.SelectionItemPattern]$pattern).Select()
    Start-Sleep -Milliseconds 700
}

function Write-SmokeDiagnostics {
    if ($null -ne $process -and -not $process.HasExited) {
        Write-Host 'Visible Local ERP windows at failure:'
        $condition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
            $process.Id)
        $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            $condition)
        foreach ($window in $windows) {
            Write-Host " - $($window.Current.Name)"
            $errorText = $window.FindFirst(
                [System.Windows.Automation.TreeScope]::Descendants,
                [System.Windows.Automation.PropertyCondition]::new(
                    [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
                    'ErrorText'))
            if ($null -ne $errorText -and -not [string]::IsNullOrWhiteSpace($errorText.Current.Name)) {
                Write-Host "   ErrorText: $($errorText.Current.Name)"
            }
        }
    }

    $logFile = Get-ChildItem (Join-Path $smokeRoot 'Logs\localerp-*.log') -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if ($null -ne $logFile) {
        Write-Host 'Local ERP smoke log:'
        Get-Content $logFile.FullName | ForEach-Object { Write-Host $_ }
    }
}

try {
    Remove-Item $smokeRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path (Split-Path $dbPath -Parent) -Force | Out-Null
    $env:LOCALERP_DATA_ROOT = $smokeRoot

    $connection = "Data Source=$dbPath;Mode=ReadWriteCreate;Foreign Keys=True;Default Timeout=15"
    & dotnet ef database update --project (Join-Path $projectRoot 'src\LocalErp.Persistence\LocalErp.Persistence.csproj') --startup-project (Join-Path $projectRoot 'src\LocalErp.Persistence\LocalErp.Persistence.csproj') --context ErpDbContext --connection $connection --configuration $Configuration --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Failed to prepare the isolated smoke database.' }
    if (-not (Test-Path $appExe)) { throw "Desktop executable not found at '$appExe'. Build the solution first." }

    $process = Start-Process -FilePath $appExe -PassThru
    $bootstrap = Wait-Window $process.Id 'Configuração inicial — Local ERP'

    Set-Value (Find-ByAutomationId $bootstrap 'UsernameTextBox') 'ci-smoke-admin'
    Set-Value (Find-ByAutomationId $bootstrap 'PasswordInput') 'SmokePassword-2026!'
    Set-Value (Find-ByAutomationId $bootstrap 'ConfirmPasswordInput') 'SmokePassword-2026!'
    Invoke-Element (Find-ByAutomationId $bootstrap 'CreateButton')

    # Bootstrap creates the administrator and authenticates the same session, so Main opens directly.
    $main = Wait-Window $process.Id 'Local ERP'
    Invoke-Button $main 'Compras'
    Start-Sleep -Milliseconds 800
    Select-Tab $main 'Solicitações'
    Select-Tab $main 'Cotações'
    Select-Tab $main 'Pedidos'

    # Logout exercises the unauthenticated route and then an explicit credential sign-in.
    Invoke-Button $main 'Sair'
    $login = Wait-Window $process.Id 'Entrar — Local ERP'
    Set-Value (Find-ByAutomationId $login 'UsernameTextBox') 'ci-smoke-admin'
    Set-Value (Find-ByAutomationId $login 'PasswordInput') 'SmokePassword-2026!'
    Invoke-Element (Find-ByAutomationId $login 'SignInButton')

    $mainAfterLogin = Wait-Window $process.Id 'Local ERP'
    Invoke-Button $mainAfterLogin 'Sair'
    $loginAfterSecondLogout = Wait-Window $process.Id 'Entrar — Local ERP'
    Invoke-Element (Find-ByAutomationId $loginAfterSecondLogout 'CancelButton')

    if (-not $process.WaitForExit(10000)) {
        throw 'Local ERP did not exit after the logout/login cancel smoke path.'
    }
    if ($process.ExitCode -ne 0) { throw "Local ERP exited with code $($process.ExitCode)." }

    $logFile = Get-ChildItem (Join-Path $smokeRoot 'Logs\localerp-*.log') -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if ($null -eq $logFile) { throw 'Smoke log file was not created.' }
    $log = Get-Content $logFile.FullName -Raw
    foreach ($route in @('AdministratorBootstrapRequired', 'SignInRequired', 'Main')) {
        $pattern = 'Startup access route resolved as.*' + [Regex]::Escape($route)
        if ($log -notmatch $pattern) {
            throw "Expected startup route '$route' was not recorded."
        }
    }
    if ([Regex]::Matches($log, 'Startup access route resolved as.*Main').Count -lt 2) {
        throw 'The authenticated Main route was not reached both after bootstrap and explicit sign-in.'
    }
    if ($log -match '\[FTL\]') { throw 'Fatal log entry detected during WPF smoke.' }

    Write-Host 'WPF smoke passed: bootstrap -> Main -> Compras tabs -> logout -> login -> Main -> logout.'
}
catch {
    Write-Host "WPF smoke failed: $($_.Exception.Message)"
    Write-SmokeDiagnostics
    throw
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
    $env:LOCALERP_DATA_ROOT = $previousRoot
    Remove-Item $smokeRoot -Recurse -Force -ErrorAction SilentlyContinue
}
