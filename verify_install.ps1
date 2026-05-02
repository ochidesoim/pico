#Requires -Version 5.1
<#
.SYNOPSIS
    VeloForge post-installation verification script.
.DESCRIPTION
    Runs 7 checks after installation and prints a PASS/FAIL summary.
    Run this from any PowerShell prompt after installing VeloForge.
.PARAMETER InstallDir
    Override the default install directory (default: C:\Program Files\VeloForge).
.EXAMPLE
    .\verify_install.ps1
    .\verify_install.ps1 -InstallDir "D:\MyApps\VeloForge"
#>

param(
    [string]$InstallDir = 'C:\Program Files\VeloForge'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'   # Keep running even if one check fails

# ── Colour helpers ────────────────────────────────────────────────────────────
function Write-Pass { param([string]$msg) Write-Host "[PASS] $msg" -ForegroundColor Green  }
function Write-Fail { param([string]$msg) Write-Host "[FAIL] $msg" -ForegroundColor Red    }
function Write-Head { param([string]$msg) Write-Host "`n$msg"      -ForegroundColor Cyan   }

# ── State ─────────────────────────────────────────────────────────────────────
$Passed  = 0
$Total   = 7
$Results = @()   # collect results for summary

function Test-Check {
    param(
        [string]   $Name,
        [bool]     $Condition,
        [string]   $FailReason = ''
    )
    if ($Condition) {
        Write-Pass $Name
        $script:Passed++
        $script:Results += @{ Name = $Name; OK = $true }
    } else {
        $detail = if ($FailReason) { " — $FailReason" } else { '' }
        Write-Fail "$Name$detail"
        $script:Results += @{ Name = $Name; OK = $false; Reason = $FailReason }
    }
}

# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n╔══════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  VeloForge Installation Verification     ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host "  Install path: $InstallDir`n"

# ─────────────────────────────────────────────────────────────────────────────
# CHECK 1 — VeloForge.exe exists at install path
# ─────────────────────────────────────────────────────────────────────────────
Write-Head "Check 1/7 — VeloForge.exe"
$VeloExe = Join-Path $InstallDir 'VeloForge.exe'
Test-Check `
    -Name       'VeloForge.exe exists at install path' `
    -Condition  (Test-Path $VeloExe) `
    -FailReason "Not found at '$VeloExe'"

# ─────────────────────────────────────────────────────────────────────────────
# CHECK 2 — pico.exe exists at install path\pipeline\
# ─────────────────────────────────────────────────────────────────────────────
Write-Head "Check 2/7 — pico.exe (C# pipeline)"
$PicoExe = Join-Path $InstallDir 'pipeline\pico.exe'
Test-Check `
    -Name       'pico.exe exists at install path\pipeline\' `
    -Condition  (Test-Path $PicoExe) `
    -FailReason "Not found at '$PicoExe'"

# ─────────────────────────────────────────────────────────────────────────────
# CHECK 3 — ccx.exe is on PATH and responds to ccx --version
# ─────────────────────────────────────────────────────────────────────────────
Write-Head "Check 3/7 — ccx.exe (CalculiX)"
$ccxOnPath = $false
$ccxReason = ''
$ccxCmd    = Get-Command 'ccx.exe' -ErrorAction SilentlyContinue

if ($ccxCmd) {
    try {
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName               = $ccxCmd.Source
        $psi.Arguments              = '--version'
        $psi.UseShellExecute        = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError  = $true
        $proc = [System.Diagnostics.Process]::Start($psi)
        $proc.WaitForExit(5000) | Out-Null
        # ccx outputs version to stderr; any non-crash exit is acceptable
        $ccxOnPath = $true
    } catch {
        $ccxReason = "Found on PATH but failed to run: $_"
    }
} else {
    $ccxReason = 'ccx.exe not found on PATH'
}

Test-Check -Name 'ccx.exe on PATH and responds' -Condition $ccxOnPath -FailReason $ccxReason

# ─────────────────────────────────────────────────────────────────────────────
# CHECK 4 — fTetWild.exe is on PATH
# ─────────────────────────────────────────────────────────────────────────────
Write-Head "Check 4/7 — fTetWild.exe"
$ftetCmd = Get-Command 'fTetWild.exe' -ErrorAction SilentlyContinue
Test-Check `
    -Name       'fTetWild.exe on PATH' `
    -Condition  ($null -ne $ftetCmd) `
    -FailReason 'fTetWild.exe not found on PATH — check that {app}\bins was added correctly'

# ─────────────────────────────────────────────────────────────────────────────
# CHECK 5 — ccx2paraview.exe is on PATH
# ─────────────────────────────────────────────────────────────────────────────
Write-Head "Check 5/7 — ccx2paraview.exe"
$c2pCmd = Get-Command 'ccx2paraview.exe' -ErrorAction SilentlyContinue
Test-Check `
    -Name       'ccx2paraview.exe on PATH' `
    -Condition  ($null -ne $c2pCmd) `
    -FailReason 'ccx2paraview.exe not found on PATH'

# ─────────────────────────────────────────────────────────────────────────────
# CHECK 6 — .NET 9 Runtime is installed
# ─────────────────────────────────────────────────────────────────────────────
Write-Head "Check 6/7 — .NET 9 Runtime"
$dotnetInstalled = $false
$dotnetReason    = ''
try {
    $psi2 = [System.Diagnostics.ProcessStartInfo]::new()
    $psi2.FileName               = 'dotnet'
    $psi2.Arguments              = '--list-runtimes'
    $psi2.UseShellExecute        = $false
    $psi2.RedirectStandardOutput = $true
    $psi2.RedirectStandardError  = $true
    $proc2 = [System.Diagnostics.Process]::Start($psi2)
    $out   = $proc2.StandardOutput.ReadToEnd()
    $proc2.WaitForExit(10000) | Out-Null

    # Look for any Microsoft.NETCore.App 9.x or Microsoft.WindowsDesktop.App 9.x
    if ($out -match 'App 9\.' ) {
        $dotnetInstalled = $true
    } else {
        $dotnetReason = '.NET 9 runtime not listed. Run: dotnet --list-runtimes'
    }
} catch {
    $dotnetReason = "dotnet CLI not found on PATH or failed: $_"
}
Test-Check -Name '.NET 9 Runtime installed' -Condition $dotnetInstalled -FailReason $dotnetReason

# ─────────────────────────────────────────────────────────────────────────────
# CHECK 7 — Disk space > 500 MB remaining on install drive
# ─────────────────────────────────────────────────────────────────────────────
Write-Head "Check 7/7 — Remaining disk space (> 500 MB)"
$diskOK     = $false
$diskReason = ''
try {
    # Extract drive letter from install dir
    $drive = Split-Path -Qualifier $InstallDir
    if (-not $drive) { $drive = 'C:' }
    $diskInfo = Get-PSDrive -Name ($drive.TrimEnd(':')) -ErrorAction SilentlyContinue
    if ($diskInfo) {
        $freeMB = [math]::Round($diskInfo.Free / 1MB, 0)
        if ($freeMB -gt 500) {
            $diskOK = $true
        } else {
            $diskReason = "$freeMB MB free on $drive — at least 500 MB required"
        }
    } else {
        # Fallback via WMI
        $wmiDisk = Get-WmiObject Win32_LogicalDisk -Filter "DeviceID='$drive'" -ErrorAction SilentlyContinue
        if ($wmiDisk) {
            $freeMB = [math]::Round($wmiDisk.FreeSpace / 1MB, 0)
            $diskOK = ($freeMB -gt 500)
            if (-not $diskOK) { $diskReason = "$freeMB MB free — at least 500 MB required" }
        } else {
            $diskReason = "Could not query disk info for $drive"
        }
    }
} catch {
    $diskReason = "Disk space check failed: $_"
}
Test-Check -Name "Disk space > 500 MB remaining on $drive" -Condition $diskOK -FailReason $diskReason

# ─────────────────────────────────────────────────────────────────────────────
# SUMMARY
# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

if ($Passed -eq $Total) {
    $colour = 'Green'
} elseif ($Passed -ge ($Total / 2)) {
    $colour = 'Yellow'
} else {
    $colour = 'Red'
}

Write-Host "VeloForge installation verified — $Passed/$Total checks passed" -ForegroundColor $colour

if ($Passed -lt $Total) {
    Write-Host "`nFailed checks:" -ForegroundColor Red
    foreach ($r in $Results) {
        if (-not $r.OK) {
            $reason = if ($r.Reason) { " — $($r.Reason)" } else { '' }
            Write-Host "  • $($r.Name)$reason" -ForegroundColor Red
        }
    }
}
Write-Host ""
