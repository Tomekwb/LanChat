# Backup-LanChat.ps1
# Standard backup to: C:\LanChatBackup\LanChat_BACKUP_YYYYMMDD_HHMMSS
# - SRC snapshot (without bin/obj/.git)
# - RUNTIME\server snapshot (full, incl. Data/Files/Updates/app)
# - MANIFEST.txt with SHA256 + size + relative path

$ErrorActionPreference = "Stop"

$SrcRoot     = "C:\LanChat\src"
$RuntimeRoot = "C:\LanChat\runtime\server"
$BackupRoot  = "C:\LanChatBackup"

$ts = Get-Date -Format "yyyyMMdd_HHmmss"
$dest = Join-Path $BackupRoot ("LanChat_BACKUP_{0}" -f $ts)

$destSrc     = Join-Path $dest "src"
$destRuntime = Join-Path $dest "runtime_server"

New-Item -ItemType Directory -Force -Path $destSrc, $destRuntime | Out-Null

function Robocopy-Mirror {
    param(
        [Parameter(Mandatory=$true)][string]$From,
        [Parameter(Mandatory=$true)][string]$To,
        [string[]]$XD = @()
    )

    $args = @(
        $From, $To,
        "/MIR",
        "/R:1", "/W:1",
        "/NFL", "/NDL", "/NP"
    )

    foreach ($x in $XD) { $args += @("/XD", $x) }

    $p = Start-Process -FilePath "robocopy.exe" -ArgumentList $args -Wait -PassThru
    # robocopy codes: 0-7 OK, >=8 error
    if ($p.ExitCode -ge 8) {
        throw "Robocopy failed (ExitCode=$($p.ExitCode)) From='$From' To='$To'"
    }
}

Write-Host "=== LanChat BACKUP ==="
Write-Host "SRC     : $SrcRoot"
Write-Host "RUNTIME : $RuntimeRoot"
Write-Host "DEST    : $dest"
Write-Host ""

if (-not (Test-Path $SrcRoot))     { throw "Missing SRC: $SrcRoot" }
if (-not (Test-Path $RuntimeRoot)) { throw "Missing RUNTIME: $RuntimeRoot" }

# 1) Copy SRC (exclude .git + bin/obj everywhere)
Write-Host "[1/3] Copy SRC..."
Robocopy-Mirror -From $SrcRoot -To $destSrc -XD @(".git","bin","obj")

# 2) Copy runtime\server (full mirror)
Write-Host "[2/3] Copy runtime\\server..."
Robocopy-Mirror -From $RuntimeRoot -To $destRuntime

# 3) Manifest
Write-Host "[3/3] Create MANIFEST.txt..."
$manifest = Join-Path $dest "MANIFEST.txt"

"=== LanChat BACKUP MANIFEST ===" | Out-File -FilePath $manifest -Encoding UTF8
"Created: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")" | Out-File -FilePath $manifest -Append -Encoding UTF8
"SRC: $SrcRoot -> $destSrc" | Out-File -FilePath $manifest -Append -Encoding UTF8
"RUNTIME: $RuntimeRoot -> $destRuntime" | Out-File -FilePath $manifest -Append -Encoding UTF8
"" | Out-File -FilePath $manifest -Append -Encoding UTF8

function Add-ManifestSection {
    param(
        [Parameter(Mandatory=$true)][string]$Title,
        [Parameter(Mandatory=$true)][string]$Root
    )

    "=== $Title ===" | Out-File -FilePath $manifest -Append -Encoding UTF8

    $files = Get-ChildItem -Path $Root -File -Recurse | Sort-Object FullName
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($Root.Length).TrimStart("\")
        $hash = (Get-FileHash -Path $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "{0}  {1,12}  {2}" -f $hash, $f.Length, $rel | Out-File -FilePath $manifest -Append -Encoding UTF8
    }
    "" | Out-File -FilePath $manifest -Append -Encoding UTF8
}

Add-ManifestSection -Title "SRC_SNAPSHOT" -Root $destSrc
Add-ManifestSection -Title "RUNTIME_SERVER_SNAPSHOT" -Root $destRuntime

Write-Host ""
Write-Host "DONE. Backup created:"
Write-Host $dest