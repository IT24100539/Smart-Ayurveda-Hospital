# Stops only the processes listening on the local dev ports, plus their child
# processes. Compatible with Windows PowerShell 5.1 and PowerShell 7.
#Requires -Version 5.1
$ErrorActionPreference = 'Continue'
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -Scope Global -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}

$Ports = @(7443, 5080, 8100, 5173, 5174)

function Get-ListeningPids {
    param([int]$Port)
    $found = @()
    $connections = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    foreach ($connection in $connections) {
        $processId = [int]$connection.OwningProcess
        if ($processId -gt 4) {
            $found += $processId
        }
    }
    return @($found | Select-Object -Unique)
}

function Get-ProcessTree {
    param([int]$ProcessId)
    $ordered = New-Object System.Collections.Generic.List[int]
    $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$ProcessId" -ErrorAction SilentlyContinue)
    foreach ($child in $children) {
        $nested = @(Get-ProcessTree -ProcessId ([int]$child.ProcessId))
        foreach ($nestedId in $nested) {
            if (-not $ordered.Contains($nestedId)) {
                $ordered.Add($nestedId)
            }
        }
    }
    if (-not $ordered.Contains($ProcessId)) {
        $ordered.Add($ProcessId)
    }
    return @($ordered)
}

$listeners = @{}
$trees = New-Object System.Collections.Generic.List[int]
foreach ($port in $Ports) {
    $pids = @(Get-ListeningPids -Port $port)
    $listeners[$port] = $pids
    foreach ($processId in $pids) {
        if ($processId -eq $PID) {
            Write-Warning "port ${port} is owned by this shell; leaving it alone"
            continue
        }
        foreach ($treeId in @(Get-ProcessTree -ProcessId $processId)) {
            if ($treeId -ne $PID -and -not $trees.Contains($treeId)) {
                $trees.Add($treeId)
            }
        }
    }
}

foreach ($processId in $trees) {
    $name = $null
    try {
        $name = (Get-Process -Id $processId -ErrorAction SilentlyContinue).ProcessName
    } catch {
        $name = $null
    }
    Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    if ($name) {
        Write-Host "stopped $name (pid $processId)"
    }
}

$busy = $false
$deadline = (Get-Date).AddSeconds(8)
foreach ($port in $Ports) {
    $before = @($listeners[$port])
    do {
        $after = @(Get-ListeningPids -Port $port)
        if ($after.Count -eq 0) { break }
        if ((Get-Date) -ge $deadline) { break }
        Start-Sleep -Milliseconds 300
    } while ($true)

    if ($before.Count -eq 0 -and $after.Count -eq 0) {
        Write-Host "port ${port}: already free"
    } elseif ($after.Count -eq 0) {
        Write-Host "port ${port}: freed"
    } else {
        Write-Warning "port ${port} is still busy (pid $($after -join ', '))"
        $busy = $true
    }
}

if ($busy) { exit 1 }
exit 0
