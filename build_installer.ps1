#Requires -Version 5.1
<#
.SYNOPSIS
    VeloForge full build pipeline - produces VeloForge_Setup.exe
.DESCRIPTION
    Runs all 5 build steps in sequence with error handling.
    Any step failure prints a clear error and stops the script.
    Does NOT require admin rights.
.NOTES
    Prerequisites: Node.js, .NET 9 SDK, Python 3.x, PyInstaller, Inno Setup 6.x
    See README_BUILD.md for full setup instructions.
#>

# ==============================================================================
#  CONFIGURABLE VARIABLES  — edit these when upgrading tools or changing paths
# ==============================================================================

# App metadata
$AppName    = 'VeloForge'
$AppVersion = '0.1.0-alpha'

# Paths to simulation binaries RELATIVE to the repo root ($RootDir)
# Update $CcxSubPath when a new CalculiX release is downloaded
$FTetWildSubPath = 'fTetWild\build\Release\FloatTetwild_bin.exe'
$CcxSubPath      = 'calculix\CalculiX-2.21.0-win-x64\bin\ccx.exe'
# ccx2paraview is expected on PATH (not bundled in repo)
$Ccx2ParaviewBin = 'ccx2paraview.exe'

# SHA-256 checksums for security-critical binaries
# Run: (Get-FileHash <path> -Algorithm SHA256).Hash  to get these values
$ChecksumFTetWild = 'BA6A862777403603E47B1544A3479D2FDF4C14FD7EBF32A7961F96F7D62004C5'
$ChecksumCcx      = '4DE9F3786571E2710C833B508D53B7DFB8DF8F8F4B0FABD94153C372A5E5F19A'

# VC++ Redist download URL (HTTPS only)
$VcRedistUrl = 'https://aka.ms/vs/17/release/vc_redist.x64.exe'

# ==============================================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Resolve root directory dynamically — works from any drive or folder
$RootDir = $PSScriptRoot
if (-not $RootDir) { $RootDir = Split-Path -Parent $MyInvocation.MyCommand.Path }

# ── Helper: run a process safely without UseShellExecute ─────────────────────
function Invoke-Tool {
    param(
        [string]   $Executable,
        [string[]] $Arguments,
        [string]   $WorkingDir = $RootDir
    )
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName               = $Executable
    $psi.WorkingDirectory       = $WorkingDir
    $psi.UseShellExecute        = $false   # Security: never UseShellExecute
    $psi.RedirectStandardOutput = $false
    $psi.RedirectStandardError  = $false
    $escapedArgs = @()
    foreach ($arg in $Arguments) {
        if ($arg -match '\s') { $escapedArgs += "`"$arg`"" }
        else { $escapedArgs += $arg }
    }
    $psi.Arguments = $escapedArgs -join ' '

    $proc = [System.Diagnostics.Process]::Start($psi)
    $proc.WaitForExit()

    if ($proc.ExitCode -ne 0) {
        throw "'$Executable $($Arguments -join ' ')' exited with code $($proc.ExitCode)"
    }
}

# ── Helper: validate install path (reject path traversal) ────────────────────
function Assert-SafePath {
    param([string]$Path)
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath -match '\.\.') {
        throw "Path traversal detected in path: '$Path'"
    }
    return $fullPath
}

# ── Helper: verify SHA-256 checksum ──────────────────────────────────────────
function Assert-Checksum {
    param(
        [string]$FilePath,
        [string]$ExpectedHash,
        [string]$Label
    )
    $actual = (Get-FileHash -Path $FilePath -Algorithm SHA256).Hash
    if ($actual -ne $ExpectedHash.ToUpper()) {
        throw "Checksum mismatch for $Label`n  Expected: $ExpectedHash`n  Got:      $actual"
    }
    Write-OK "Checksum verified: $Label"
}


Write-Host "`n+------------------------------------------+" -ForegroundColor Yellow
Write-Host "|     VeloForge Installer Build Pipeline   |" -ForegroundColor Yellow
Write-Host "+------------------------------------------+`n" -ForegroundColor Yellow

# ------------------------------------------------------------------------------
#  STEP 1 - Build Next.js frontend  ->  web\out\
# ------------------------------------------------------------------------------
Write-Step "1/5  Build Next.js frontend"

$WebDir = Join-Path $RootDir 'web'
if (-not (Test-Path $WebDir)) { Write-Fail "web\ directory not found at '$WebDir'"; exit 1 }

try {
    Write-Info "npm install..."
    Invoke-Tool -Executable 'cmd.exe' -Arguments @('/c', 'npm', 'install') -WorkingDir $WebDir

    Write-Info "npm run build..."
    Invoke-Tool -Executable 'cmd.exe' -Arguments @('/c', 'npm', 'run', 'build') -WorkingDir $WebDir

    $OutDir = Join-Path $WebDir 'out'
    if (-not (Test-Path $OutDir)) { throw "web\out\ not produced - Next.js static export may have failed" }
    Write-OK "Next.js static export ready at web\out\"
}
catch {
    Write-Fail "STEP 1 FAILED: $_"
    exit 1
}

# ------------------------------------------------------------------------------
#  STEP 2 - Publish C# pipeline  ->  dist\pipeline\
# ------------------------------------------------------------------------------
Write-Step "2/5  Publish C# pipeline (self-contained, win-x64)"

$PipelineOut = Assert-SafePath (Join-Path $RootDir 'dist\pipeline')

try {
    Invoke-Tool -Executable 'dotnet' -Arguments @(
        'publish', 'pico.csproj',
        '--configuration', 'Release',
        '--runtime',       'win-x64',
        '--self-contained','true',
        '--output',        $PipelineOut
    ) -WorkingDir $RootDir

    $PicoExe = Join-Path $PipelineOut 'pico.exe'
    if (-not (Test-Path $PicoExe)) { throw "pico.exe not found in dist\pipeline\ after publish" }
    Write-OK "pico.exe published to dist\pipeline\"
}
catch {
    Write-Fail "STEP 2 FAILED: $_"
    exit 1
}

# ------------------------------------------------------------------------------
#  STEP 3 - Package Python GUI with PyInstaller  ->  dist\VeloForge.exe
# ------------------------------------------------------------------------------
Write-Step "3/5  Package Python GUI with PyInstaller"

try {
    Write-Info "Installing Python dependencies..."
    Invoke-Tool -Executable 'cmd.exe' -Arguments @('/c', 'python', '-m', 'pip', 'install', '--quiet', 'pyinstaller', 'pyvista', 'numpy') -WorkingDir $RootDir

    $IconPath     = Join-Path $RootDir 'assets\veloforge.ico'
    # Build add-data as absolute_src;dest_name so it works from any working dir
    $WebOutSrc    = Join-Path $RootDir 'web\out'
    $PipelineSrc  = Join-Path $RootDir 'dist\pipeline\pico.exe'
    $ConfigSrc    = Join-Path $RootDir 'configs'
    $WebOutData   = "$WebOutSrc;web\out"
    $PipelineData = "$PipelineSrc;pipeline"
    $ConfigData   = "$ConfigSrc;configs"

    Write-Info "Running PyInstaller..."
    Invoke-Tool -Executable 'cmd.exe' -Arguments @(
        '/c', 'python', '-m', 'PyInstaller',
        'gui.py',
        '--name',     'VeloForge',
        '--onefile',
        '--windowed',
        '--icon',     $IconPath,
        '--add-data', $WebOutData,
        '--add-data', $PipelineData,
        '--add-data', $ConfigData,
        '--distpath', (Join-Path $RootDir 'dist'),
        '--workpath', (Join-Path $RootDir 'build\pyinstaller'),
        '--noconfirm'
    ) -WorkingDir $RootDir

    $VeloExe = Join-Path $RootDir 'dist\VeloForge.exe'
    if (-not (Test-Path $VeloExe)) { throw "dist\VeloForge.exe not produced by PyInstaller" }
    Write-OK "VeloForge.exe packaged at dist\VeloForge.exe"
}
catch {
    Write-Fail "STEP 3 FAILED: $_"
    exit 1
}

# ------------------------------------------------------------------------------
#  STEP 4 - Collect all files into dist\  and verify checksums
# ------------------------------------------------------------------------------
Write-Step "4/5  Assemble dist\ layout and verify binary checksums"

try {
    # Create required subdirectories
    $BinsDir    = Assert-SafePath (Join-Path $RootDir 'dist\bins')
    $RedistDir  = Assert-SafePath (Join-Path $RootDir 'dist\redist')
    New-Item -ItemType Directory -Force -Path $BinsDir   | Out-Null
    New-Item -ItemType Directory -Force -Path $RedistDir | Out-Null

    # Binary source paths - built from configurable variables at top of this file
    $BinSources = @{
        'fTetWild.exe'     = Join-Path $RootDir $FTetWildSubPath
        'ccx.exe'          = Join-Path $RootDir $CcxSubPath
        'ccx2paraview.exe' = $Ccx2ParaviewBin   # resolved from PATH below
    }

    foreach ($name in $BinSources.Keys) {
        $src = $BinSources[$name]
        # Resolve from PATH if not an absolute path
        if (-not [System.IO.Path]::IsPathRooted($src)) {
            $cmdObj   = Get-Command $src -ErrorAction SilentlyContinue
            $resolved = if ($cmdObj) { $cmdObj.Source } else { $null }
            if (-not $resolved) { throw "Could not locate '$src' -- ensure it is on PATH or update BinSources in this script" }
            $src = $resolved
        }
        if (-not (Test-Path $src)) { throw "Required binary not found: $src" }

        $dst = Join-Path $BinsDir $name
        Copy-Item -Path $src -Destination $dst -Force
        Write-Info "Copied $name -> dist\bins\"
    }

    # Verify checksums for security-critical binaries
    # (Only runs if the hardcoded placeholder values have been replaced)
    if ($ChecksumFTetWild -notmatch '^REPLACE') {
        Assert-Checksum -FilePath (Join-Path $BinsDir 'fTetWild.exe') -ExpectedHash $ChecksumFTetWild -Label 'fTetWild.exe'
    } else {
        Write-Host "       [WARN] fTetWild.exe checksum not set - skipping verification" -ForegroundColor Yellow
    }
    if ($ChecksumCcx -notmatch '^REPLACE') {
        Assert-Checksum -FilePath (Join-Path $BinsDir 'ccx.exe') -ExpectedHash $ChecksumCcx -Label 'ccx.exe'
    } else {
        Write-Host "       [WARN] ccx.exe checksum not set - skipping verification" -ForegroundColor Yellow
    }

    # VC++ Redist - download over HTTPS if not already present
    $VcRedist = Join-Path $RedistDir 'vc_redist.x64.exe'
    if (-not (Test-Path $VcRedist)) {
        Write-Info "Downloading VC++ Redistributable x64 over HTTPS..."
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $VcRedistUrl -OutFile $VcRedist -UseBasicParsing
        Write-OK "Downloaded vc_redist.x64.exe"
    } else {
        Write-OK "vc_redist.x64.exe already present"
    }

    Write-OK "dist\ layout assembled"
}
catch {
    Write-Fail "STEP 4 FAILED: $_"
    exit 1
}

# ------------------------------------------------------------------------------
#  STEP 5 - Compile Inno Setup script  ->  VeloForge_Setup.exe
# ------------------------------------------------------------------------------
Write-Step "5/5  Compile Inno Setup installer"

try {
    # Locate iscc.exe (Inno Setup compiler)
    $IsccCandidates = @(
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe',
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    $Iscc = $null
    foreach ($candidate in $IsccCandidates) {
        if (Test-Path $candidate) { $Iscc = $candidate; break }
    }
    # Fall back to PATH
    if (-not $Iscc) {
        $fromPathCmd = Get-Command 'iscc.exe' -ErrorAction SilentlyContinue
        if ($fromPathCmd) { $Iscc = $fromPathCmd.Source }
    }
    if (-not $Iscc) { throw "Inno Setup compiler (ISCC.exe) not found. Install Inno Setup 6.x first." }

    $IssFile = Join-Path $RootDir 'setup.iss'
    if (-not (Test-Path $IssFile)) { throw "setup.iss not found at '$IssFile'" }

    Invoke-Tool -Executable $Iscc -Arguments @(
        "/DSourceDir=$RootDir",
        "/DOutputDir=$(Join-Path $RootDir 'dist\installer')",
        $IssFile
    ) -WorkingDir $RootDir

    # Look for the output installer
    $SetupExe = Join-Path $RootDir 'dist\installer\VeloForge_Setup.exe'
    if (-not (Test-Path $SetupExe)) {
        # Some configs output to root
        $SetupExe = Join-Path $RootDir 'VeloForge_Setup.exe'
    }
    if (Test-Path $SetupExe) {
        $sizeMB = [math]::Round((Get-Item $SetupExe).Length / 1MB, 1)
        Write-OK "VeloForge_Setup.exe produced ($sizeMB MB)"
        if ($sizeMB -gt 500) {
            Write-Host "       [WARN] Installer exceeds 500 MB target ($sizeMB MB)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "       [INFO] VeloForge_Setup.exe location may differ - check setup.iss OutputDir" -ForegroundColor Yellow
    }
}
catch {
    Write-Fail "STEP 5 FAILED: $_"
    exit 1
}

# ------------------------------------------------------------------------------
#  DONE
# ------------------------------------------------------------------------------
Write-Host "`n+------------------------------------------+" -ForegroundColor Green
Write-Host "|   BUILD COMPLETE - All 5 steps passed    |" -ForegroundColor Green
Write-Host "+------------------------------------------+" -ForegroundColor Green
Write-Host "  Installer -> dist\installer\VeloForge_Setup.exe`n" -ForegroundColor Green
