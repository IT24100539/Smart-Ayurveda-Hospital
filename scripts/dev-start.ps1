# Starts the local hospital stack in separate windows.
# Compatible with Windows PowerShell 5.1 and PowerShell 7.
#Requires -Version 5.1
param(
    [ValidateSet('backend', 'agent', 'web', 'mobile')]
    [string[]]$Only,
    [switch]$SkipStop
)

$ErrorActionPreference = 'Continue'
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -Scope Global -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}
$ProgressPreference = 'SilentlyContinue'

$Root = Split-Path -Parent $PSScriptRoot
$Selected = @('backend', 'agent', 'web')
if ($Only -and @($Only).Count -gt 0) {
    $Selected = @($Only)
}

function Test-Selected {
    param([string]$Name)
    return $Selected -contains $Name
}

function Write-Pass {
    param([string]$Message)
    Write-Host "PASS  $Message"
}

function Write-Fail {
    param([string]$Message)
    Write-Host "FAIL  $Message"
}

function Exit-Failed {
    param([string]$Message)
    Write-Fail $Message
    exit 1
}

function Test-Url {
    param(
        [string]$Url,
        [bool]$Insecure
    )
    try {
        if ($Insecure -and $PSVersionTable.PSVersion.Major -ge 6) {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -SkipCertificateCheck
            return ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400)
        }
        if ($Insecure) {
            $request = [System.Net.HttpWebRequest]::Create($Url)
            $request.Timeout = 5000
            $request.Method = 'GET'
            $request.ServerCertificateValidationCallback = { return $true }
            $webResponse = $request.GetResponse()
            try {
                $code = [int]$webResponse.StatusCode
            } finally {
                $webResponse.Close()
            }
            return ($code -ge 200 -and $code -lt 400)
        }
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
        return ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400)
    } catch {
        return $false
    }
}

function Wait-Url {
    param(
        [string]$Url,
        [bool]$Insecure,
        [int]$Seconds = 90
    )
    $deadline = (Get-Date).AddSeconds($Seconds)
    do {
        if (Test-Url -Url $Url -Insecure $Insecure) { return $true }
        if ((Get-Date) -ge $deadline) { return $false }
        Start-Sleep -Seconds 2
    } while ($true)
}

function Get-ShellExecutable {
    if ($PSVersionTable.PSEdition -eq 'Core') {
        $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
        if ($pwsh) { return $pwsh.Source }
    }
    $windowsPowerShell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    if (Test-Path -LiteralPath $windowsPowerShell) { return $windowsPowerShell }
    return 'powershell.exe'
}

function Start-DevWindow {
    param(
        [string]$Title,
        [string]$WorkingDirectory,
        [string]$Body
    )
    # Concatenate so values inside $Body are not expanded by this process.
    $command = '$Host.UI.RawUI.WindowTitle = ''' + $Title + '''' + "`r`n" +
        "Set-Location -LiteralPath '$WorkingDirectory'" + "`r`n" +
        $Body
    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($command))
    Start-Process -FilePath (Get-ShellExecutable) -WorkingDirectory $WorkingDirectory -ArgumentList @(
        '-NoExit',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-EncodedCommand', $encoded
    ) | Out-Null
}

function Set-EnvAssignment {
    param(
        [string]$Path,
        [string]$Name,
        [string]$Value,
        [switch]$OnlyIfAbsent
    )
    $pattern = '^\s*' + [regex]::Escape($Name) + '\s*='
    $lines = @()
    if (Test-Path -LiteralPath $Path) {
        $lines = @(Get-Content -LiteralPath $Path)
    }
    $hasLine = $false
    foreach ($line in $lines) {
        if ($line -match $pattern) { $hasLine = $true }
    }
    if ($OnlyIfAbsent -and $hasLine) { return }

    $written = New-Object System.Collections.Generic.List[string]
    $replaced = $false
    foreach ($line in $lines) {
        if ($line -match $pattern) {
            if (-not $replaced) {
                $written.Add("$Name=$Value")
                $replaced = $true
            }
        } else {
            $written.Add($line)
        }
    }
    if (-not $replaced) {
        $written.Add("$Name=$Value")
    }
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllLines($Path, $written.ToArray(), $utf8)
}

function New-RandomSecret {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
    } finally {
        $rng.Dispose()
    }
    return [Convert]::ToBase64String($bytes)
}

function Test-RejectedSecret {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $true }
    $lower = $Value.Trim().ToLowerInvariant()
    if ($lower -eq 'postgres' -or $lower -eq 'change_me') { return $true }
    foreach ($marker in @('change-me', 'dev-only', 'dev-internal', 'replace-me', 'replace-with-a-random', 'your_')) {
        if ($lower.Contains($marker)) { return $true }
    }
    return $false
}

function Get-NamedAssignment {
    param(
        [string]$Text,
        [string[]]$Names
    )
    foreach ($line in ($Text -split "`r?`n")) {
        $eq = $line.IndexOf('=')
        if ($eq -lt 1) { continue }
        $name = $line.Substring(0, $eq).Trim()
        $value = $line.Substring($eq + 1).Trim()
        if (($Names -contains $name) -and -not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
    }
    return $null
}

function Sync-InternalServiceKey {
    $project = Join-Path $Root 'backend\src\Hospital.Api'
    $list = & dotnet user-secrets list --project $project 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        Exit-Failed "Could not read dotnet user-secrets for backend\src\Hospital.Api. Run 'dotnet user-secrets list --project backend\src\Hospital.Api' and fix the reported error."
    }

    $key = $null
    foreach ($line in ($list -split "`r?`n")) {
        $eq = $line.IndexOf('=')
        if ($eq -lt 1) { continue }
        $name = $line.Substring(0, $eq).Trim()
        $value = $line.Substring($eq + 1).Trim()
        if ($name -eq 'InternalServiceKey' -and -not [string]::IsNullOrWhiteSpace($value)) {
            $key = $value
        }
    }

    $created = $false
    if ([string]::IsNullOrWhiteSpace($key)) {
        $key = New-RandomSecret
        & dotnet user-secrets set 'InternalServiceKey' $key --project $project *> $null
        if ($LASTEXITCODE -ne 0) {
            Exit-Failed "Could not save InternalServiceKey with dotnet user-secrets."
        }
        & dotnet user-secrets set 'InternalService:Key' $key --project $project *> $null
        if ($LASTEXITCODE -ne 0) {
            Exit-Failed "Could not save InternalService:Key with dotnet user-secrets."
        }
        $created = $true
    }

    $envFile = Join-Path $Root 'agent-service\.env'
    $envDir = Split-Path -Parent $envFile
    if (-not (Test-Path -LiteralPath $envDir)) {
        Exit-Failed "agent-service directory is missing at $envDir"
    }
    Set-EnvAssignment -Path $envFile -Name 'AGENT_INTERNAL_SERVICE_KEY' -Value $key
    Set-EnvAssignment -Path $envFile -Name 'AGENT_OLLAMA_TIMEOUT_SECONDS' -Value '120' -OnlyIfAbsent

    Push-Location $Root
    & git check-ignore -q -- 'agent-service/.env' | Out-Null
    $ignored = ($LASTEXITCODE -eq 0)
    Pop-Location
    if (-not $ignored) {
        $ignorePath = Join-Path $Root '.gitignore'
        $ignoreLines = @()
        if (Test-Path -LiteralPath $ignorePath) {
            $ignoreLines = @(Get-Content -LiteralPath $ignorePath)
        }
        $already = $false
        foreach ($line in $ignoreLines) {
            if ($line.Trim() -eq 'agent-service/.env') { $already = $true }
        }
        if (-not $already) {
            [System.IO.File]::AppendAllText($ignorePath, "`r`nagent-service/.env`r`n")
        }
        Push-Location $Root
        & git check-ignore -q -- 'agent-service/.env' | Out-Null
        $ignored = ($LASTEXITCODE -eq 0)
        Pop-Location
        if (-not $ignored) {
            Exit-Failed "agent-service/.env is not git-ignored. Add it to .gitignore before starting."
        }
    }

    if ($created) {
        Write-Host 'key: created'
    } else {
        Write-Host 'key: synced'
    }
}

function Sync-AgentSharedSecret {
    $project = Join-Path $Root 'backend\src\Hospital.Api'
    $list = & dotnet user-secrets list --project $project 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        Exit-Failed "Could not read dotnet user-secrets for backend\src\Hospital.Api. Run 'dotnet user-secrets list --project backend\src\Hospital.Api' and fix the reported error."
    }

    $rootEnv = Join-Path $Root '.env'
    $agentEnv = Join-Path $Root 'agent-service\.env'
    $secret = Get-NamedAssignment -Text $list -Names @('AgentService:SharedSecret', 'AGENT_SHARED_SECRET')
    if (Test-RejectedSecret $secret) {
        $secret = $null
    }
    if ([string]::IsNullOrWhiteSpace($secret) -and (Test-Path -LiteralPath $agentEnv)) {
        $fromAgent = Get-NamedAssignment -Text ((Get-Content -LiteralPath $agentEnv) -join "`n") -Names @('AGENT_SHARED_SECRET')
        if (-not (Test-RejectedSecret $fromAgent)) { $secret = $fromAgent }
    }
    if ([string]::IsNullOrWhiteSpace($secret) -and (Test-Path -LiteralPath $rootEnv)) {
        $fromRoot = Get-NamedAssignment -Text ((Get-Content -LiteralPath $rootEnv) -join "`n") -Names @('AGENT_SHARED_SECRET')
        if (-not (Test-RejectedSecret $fromRoot)) { $secret = $fromRoot }
    }

    $created = $false
    if ([string]::IsNullOrWhiteSpace($secret)) {
        $secret = New-RandomSecret
        $created = $true
    }

    & dotnet user-secrets set 'AgentService:SharedSecret' $secret --project $project *> $null
    if ($LASTEXITCODE -ne 0) {
        Exit-Failed "Could not save AgentService:SharedSecret with dotnet user-secrets."
    }

    if (Test-Path -LiteralPath $rootEnv) {
        Set-EnvAssignment -Path $rootEnv -Name 'AGENT_SHARED_SECRET' -Value $secret
    }
    $agentDir = Split-Path -Parent $agentEnv
    if (-not (Test-Path -LiteralPath $agentDir)) {
        Exit-Failed "agent-service directory is missing at $agentDir"
    }
    Set-EnvAssignment -Path $agentEnv -Name 'AGENT_SHARED_SECRET' -Value $secret

    if ($created) {
        Write-Host 'agent-secret: created'
    } else {
        Write-Host 'agent-secret: synced'
    }
}

if (-not $SkipStop) {
    $stopScript = Join-Path $PSScriptRoot 'dev-stop.ps1'
    & (Get-ShellExecutable) -NoProfile -ExecutionPolicy Bypass -File $stopScript
    if ($LASTEXITCODE -ne 0) {
        Exit-Failed "A dev port is still busy. Close the process named in the warning above, then run scripts\dev-start.ps1 again."
    }
}

$postgres = Test-NetConnection -ComputerName 127.0.0.1 -Port 5432 -WarningAction SilentlyContinue
if ($postgres.TcpTestSucceeded) {
    Write-Pass 'PostgreSQL localhost:5432'
} else {
    Exit-Failed 'PostgreSQL is not accepting connections on localhost:5432. Start the PostgreSQL service, then run scripts\dev-start.ps1 again.'
}

$ollamaTags = $null
try {
    $ollamaTags = Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/tags' -TimeoutSec 10
} catch {
    $ollamaTags = $null
}
if ($null -eq $ollamaTags) {
    Exit-Failed 'Ollama is not reachable at http://127.0.0.1:11434. Start Ollama, then run scripts\dev-start.ps1 again.'
}
$hasModel = $false
$models = @($ollamaTags.models)
foreach ($model in $models) {
    $modelName = [string]$model.name
    if ($modelName.StartsWith('llama3.1')) { $hasModel = $true }
}
if ($hasModel) {
    Write-Pass 'Ollama model llama3.1'
} else {
    Exit-Failed 'No Ollama model whose name starts with llama3.1 is installed. Run: ollama pull llama3.1'
}

& dotnet dev-certs https --check --trust *> $null
if ($LASTEXITCODE -eq 0) {
    Write-Pass 'HTTPS dev certificate is trusted'
} else {
    Write-Host 'WARN  HTTPS dev certificate is not trusted. Run: dotnet dev-certs https --trust'
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Exit-Failed 'dotnet is not on PATH. Install the .NET 8 SDK and open a new terminal.'
}

Sync-InternalServiceKey
Sync-AgentSharedSecret

if ((Test-Selected 'agent') -and -not (Test-Path -LiteralPath (Join-Path $Root 'agent-service\.venv\Scripts\python.exe'))) {
    Exit-Failed 'The agent virtualenv is missing. From agent-service run: python -m venv .venv; .\.venv\Scripts\Activate.ps1; pip install -r requirements.txt'
}
if ((Test-Selected 'web') -and -not (Get-Command npm.cmd -ErrorAction SilentlyContinue)) {
    Exit-Failed 'npm is not on PATH. Install Node.js 22 and open a new terminal.'
}
if ((Test-Selected 'mobile') -and -not (Get-Command flutter -ErrorAction SilentlyContinue)) {
    Exit-Failed 'flutter is not on PATH. Install Flutter and open a new terminal.'
}

$rows = New-Object System.Collections.Generic.List[object]
$pending = New-Object System.Collections.Generic.List[object]

if (Test-Selected 'backend') {
    $backendDir = Join-Path $Root 'backend'
    Start-DevWindow -Title 'sah-backend' -WorkingDirectory $backendDir -Body @'
$env:AgentService__BaseUrl = 'http://127.0.0.1:8100'
dotnet run --project src/Hospital.Api
'@
    $pending.Add([pscustomobject]@{ Service = 'backend'; Url = 'https://localhost:7443'; Health = 'https://localhost:7443/api/health'; Insecure = $true }) | Out-Null
}

if (Test-Selected 'agent') {
    $agentDir = Join-Path $Root 'agent-service'
    Start-DevWindow -Title 'sah-agent' -WorkingDirectory $agentDir -Body @'
$env:AGENT_PORT = '8100'
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
. .\.venv\Scripts\Activate.ps1
python run.py
'@
    $pending.Add([pscustomobject]@{ Service = 'agent'; Url = 'http://127.0.0.1:8100'; Health = 'http://127.0.0.1:8100/docs'; Insecure = $false }) | Out-Null
}

if (Test-Selected 'web') {
    $webDir = Join-Path $Root 'web-staff'
    Start-DevWindow -Title 'sah-web' -WorkingDirectory $webDir -Body @'
if (-not (Test-Path -LiteralPath 'node_modules')) { npm.cmd install }
npm.cmd run dev
'@
    $pending.Add([pscustomobject]@{ Service = 'web'; Url = 'http://localhost:5173'; Health = 'http://localhost:5173'; Insecure = $false }) | Out-Null
}

if (Test-Selected 'mobile') {
    $mobileDir = Join-Path $Root 'mobile-patient'
    Start-DevWindow -Title 'sah-mobile' -WorkingDirectory $mobileDir -Body @'
flutter run -d web-server --web-hostname localhost --web-port 5174 --dart-define=API_BASE_URL=https://localhost:7443/api
'@
    $pending.Add([pscustomobject]@{ Service = 'mobile'; Url = 'http://localhost:5174'; Health = 'http://localhost:5174'; Insecure = $false }) | Out-Null
}

foreach ($item in $pending) {
    $up = Wait-Url -Url $item.Health -Insecure $item.Insecure -Seconds 90
    $status = 'DOWN'
    if ($up) { $status = 'UP' }
    $rows.Add([pscustomobject]@{ Service = $item.Service; Url = $item.Url; Status = $status }) | Out-Null
}

try {
    $warmBody = '{"model":"llama3.1","prompt":"hi","stream":false}'
    Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/generate' -Method Post -Body $warmBody -ContentType 'application/json' -TimeoutSec 180 | Out-Null
    Write-Host 'ollama: warmed'
} catch {
    Write-Host 'WARN  Ollama warm-up did not finish. The first feedback call may be slow.'
}

Write-Host ''
Write-Host ('{0,-10} {1,-32} {2}' -f 'Service', 'URL', 'Status')
Write-Host ('{0,-10} {1,-32} {2}' -f '-------', '---', '------')
foreach ($row in $rows) {
    Write-Host ('{0,-10} {1,-32} {2}' -f $row.Service, $row.Url, $row.Status)
}

$down = $false
foreach ($row in $rows) {
    if ($row.Status -ne 'UP') { $down = $true }
}
if ($down) { exit 1 }
exit 0
