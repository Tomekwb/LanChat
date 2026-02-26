#requires -Version 7.0
<#
Deploy-LanChatServer.ps1
Build + publish LanChatServer do runtime\server\app i restart usługi Windows "LanChatServer".

Założenia:
- repo:   C:\LanChat\src
- runtime: C:\LanChat\runtime\server
- binarka serwera (publish): C:\LanChat\runtime\server\app\LanChatServer.exe
- usługa Windows: LanChatServer
- serwer nasłuchuje na: http://0.0.0.0:5001

Uruchamiaj jako Administrator (do stop/start usługi).
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

param(
    [string]$RepoServerDir   = "C:\LanChat\src\LanChatServer",
    [string]$RuntimeRoot     = "C:\LanChat\runtime\server",
    [string]$PublishDir      = "C:\LanChat\runtime\server\app",
    [string]$ServiceName     = "LanChatServer",
    [string]$Runtime         = "win-x64",
    [int]$Port               = 5001,
    [switch]$SkipServiceRestart,
    [switch]$NoSelfContained
)

function Write-Log([string]$msg) {
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$ts] $msg"
}

function Assert-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p  = New-Object Security.Principal.WindowsPrincipal($id)
    if (-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Uruchom PowerShell jako Administrator (wymagane do stop/start usługi)."
    }
}

function Wait-ServiceState([string]$name, [string]$wanted, [int]$timeoutSec = 30) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
        if ($null -eq $svc) { throw "Nie znaleziono usługi '$name'." }
        if ($svc.Status.ToString().Equals($wanted, [StringComparison]::OrdinalIgnoreCase)) { return }
        Start-Sleep -Milliseconds 300
    }
    throw "Timeout: usługa '$name' nie osiągnęła stanu '$wanted' w ${timeoutSec}s."
}

function Test-HttpLocal([int]$port) {
    try {
        # curl.exe jest stabilny do szybkiego pinga
        $out = & curl.exe -s ("http://127.0.0.1:{0}/" -f $port)
        return $out
    } catch {
        return ""
    }
}

Assert-Admin

Write-Log "== LanChatServer DEPLOY =="

# 1) Walidacja ścieżek
if (-not (Test-Path $RepoServerDir)) { throw "Brak katalogu RepoServerDir: $RepoServerDir" }
if (-not (Test-Path $RuntimeRoot))   { throw "Brak katalogu RuntimeRoot: $RuntimeRoot" }
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

$exePath = Join-Path $PublishDir "LanChatServer.exe"
$backupDir = Join-Path $RuntimeRoot "backup"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

Write-Log "RepoServerDir : $RepoServerDir"
Write-Log "PublishDir    : $PublishDir"
Write-Log "ServiceName   : $ServiceName"
Write-Log "Port          : $Port"

# 2) Stop usługi (żeby nie blokowała plików EXE)
if (-not $SkipServiceRestart) {
    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($null -eq $svc) { throw "Nie znaleziono usługi '$ServiceName'. Najpierw ją utwórz (sc.exe create ...)." }

    if ($svc.Status -eq "Running") {
        Write-Log "Stop: $ServiceName"
        Stop-Service -Name $ServiceName -Force
        Wait-ServiceState -name $ServiceName -wanted "Stopped" -timeoutSec 45
    } else {
        Write-Log "Usługa nie działa (Status=$($svc.Status)). OK."
    }
} else {
    Write-Log "SkipServiceRestart=ON (nie zatrzymuję/nie uruchamiam usługi)."
}

# 3) Backup poprzedniej binarki (jeśli istnieje)
if (Test-Path $exePath) {
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $bak = Join-Path $backupDir ("LanChatServer_{0}.exe" -f $stamp)
    Write-Log "Backup: $exePath -> $bak"
    Copy-Item -Force $exePath $bak
}

# 4) Publish
Write-Log "Publish: dotnet publish (Release, $Runtime)"
Push-Location $RepoServerDir
try {
    $args = @("publish", "-c", "Release", "-r", $Runtime)

    if ($NoSelfContained) {
        # Framework-dependent (mniejszy output, wymaga runtime .NET na serwerze)
        $args += @("--self-contained", "false")
    } else {
        # Self-contained + single file EXE (najprostsze dla usługi)
        $args += @("--self-contained", "true", "/p:PublishSingleFile=true")
    }

    $args += @("-o", $PublishDir)

    & dotnet @args | ForEach-Object { Write-Host $_ }
}
finally {
    Pop-Location
}

if (-not (Test-Path $exePath)) {
    throw "Publish zakończony, ale nie ma EXE: $exePath"
}
Write-Log "OK: jest $exePath"

# 5) Start usługi
if (-not $SkipServiceRestart) {
    Write-Log "Start: $ServiceName"
    Start-Service -Name $ServiceName
    Wait-ServiceState -name $ServiceName -wanted "Running" -timeoutSec 45
}

# 6) Smoke test localhost
Write-Log "Smoke test: curl http://127.0.0.1:$Port/"
$out = Test-HttpLocal -port $Port
if ([string]::IsNullOrWhiteSpace($out)) {
    Write-Log "UWAGA: brak odpowiedzi HTTP (localhost). Sprawdź logi usługi i czy port $Port nasłuchuje."
} else {
    Write-Log "HTTP OK: $out"
}

Write-Log "DEPLOY DONE."