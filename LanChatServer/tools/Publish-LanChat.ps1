# FILE_VERSION: Publish-LanChat.ps1 v1.1 (2026-02-20)
# Purpose: Publish client+updater -> ZIP + version.json (runtime Updates), with SHA256 and version verification.
# Note: ProductVersion may include "+<git-hash>" (InformationalVersion). We compare only the prefix before '+'.

#requires -Version 5.1
[CmdletBinding()]
param(
  # === NOWE DOMYŚLNE ŚCIEŻKI (po przeniesieniu do repo) ===
  [string]$ClientProjDir  = "C:\LanChat\src\LanChatClient\LanChatClient",
  [string]$UpdaterProjDir = "C:\LanChat\src\LanChatClient\LanChatUpdater",

  # === RUNTIME (poza repo) ===
  [string]$UpdatesDir     = "C:\LanChat\runtime\server\Updates",

  [string]$BaseUpdatesUrl = "http://192.168.64.233:5001/updates",
  [string]$ExeName        = "LanChatClient.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Log([string]$msg) {
  $ts = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
  $line = "[$ts] $msg"
  Write-Host $line
  Add-Content -Path (Join-Path $UpdatesDir "publish_log.txt") -Value $line -Encoding UTF8
}

function Read-CsprojVersion([string]$csprojPath) {
  $xml = [xml](Get-Content -Path $csprojPath -Raw -Encoding UTF8)
  $pg = $xml.Project.PropertyGroup | Select-Object -First 1
  if (-not $pg.Version) { throw "Missing <Version> in csproj: $csprojPath" }
  return [string]$pg.Version
}

function Set-CsprojVersion([string]$csprojPath, [string]$ver) {
  $xml = [xml](Get-Content -Path $csprojPath -Raw -Encoding UTF8)
  $pg = $xml.Project.PropertyGroup | Select-Object -First 1

  $pg.Version = $ver
  $pg.AssemblyVersion = "$ver.0"
  $pg.FileVersion = "$ver.0"

  $xml.Save($csprojPath)
}

function Bump-Patch([string]$ver) {
  $v = [version]$ver
  return "{0}.{1}.{2}" -f $v.Major, $v.Minor, ($v.Build + 1)
}

function Run-DotnetPublish([string]$projDir) {
  Push-Location $projDir
  try {
    Write-Host ""
    Write-Host "== dotnet publish: $projDir =="
    & dotnet publish -c Release -r win-x64 --self-contained true
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish FAILED ($projDir) exit=$LASTEXITCODE" }
  }
  finally { Pop-Location }
}

function Get-PublishDir([string]$projDir) {
  $p = Join-Path $projDir "bin\Release\net8.0-windows\win-x64\publish"
  if (!(Test-Path $p)) { throw "Missing publish dir: $p" }
  return $p
}

function Normalize-VersionString([string]$v) {
  if ([string]::IsNullOrWhiteSpace($v)) { return $v }
  # InformationalVersion often is "1.0.8+<hash>" -> we compare only prefix
  return ($v.Split('+')[0]).Trim()
}

function Get-ExeProductVersionNormalized([string]$exePath) {
  if (!(Test-Path $exePath)) { throw "Missing EXE for verification: $exePath" }
  $pv = (Get-Item $exePath).VersionInfo.ProductVersion
  return (Normalize-VersionString $pv)
}

# --- LOCK (avoid running twice) ---
New-Item -ItemType Directory -Path $UpdatesDir -Force | Out-Null
$lockPath = Join-Path $UpdatesDir "publish.lock"
$lockStream = $null
try {
  $lockStream = [System.IO.File]::Open($lockPath, 'OpenOrCreate', 'ReadWrite', 'None')
} catch {
  throw "Publish already running (lock: $lockPath). If this is wrong, delete lock file manually."
}

try {
  Write-Host ""
  Write-Host "=== LanChat PUBLISH (client+updater -> ZIP + version.json) ==="
  Write-Host "ClientProjDir : $ClientProjDir"
  Write-Host "UpdaterProjDir: $UpdaterProjDir"
  Write-Host "UpdatesDir    : $UpdatesDir"
  Write-Host "BaseUpdatesUrl: $BaseUpdatesUrl"
  Write-Host ""

  $clientCsproj = Join-Path $ClientProjDir "LanChatClient.csproj"
  if (!(Test-Path $clientCsproj)) { throw "Missing client csproj: $clientCsproj" }

  $currentVer = Read-CsprojVersion $clientCsproj
  Write-Host "Current csproj version: $currentVer"

  $suggested = $currentVer
  try { $suggested = Bump-Patch $currentVer } catch { }

  $mode = Read-Host "Version: A=auto bump (suggested $suggested) / M=manual [A/M]"
  $mode = ($mode ?? "").Trim().ToUpperInvariant()
  if ($mode -ne "M") { $mode = "A" }

  if ($mode -eq "A") {
    $confirm = Read-Host "Set version to $suggested automatically? [Y/N]"
    if (($confirm ?? "").Trim().ToUpperInvariant() -eq "Y") {
      $targetVer = $suggested
    } else {
      $targetVer = Read-Host "Enter version manually (e.g. 1.0.8)"
    }
  } else {
    $targetVer = Read-Host "Enter version manually (e.g. 1.0.8)"
  }

  $targetVer = ($targetVer ?? "").Trim()
  if ([string]::IsNullOrWhiteSpace($targetVer)) { throw "Version cannot be empty." }

  $verObj = $null
  if (-not [version]::TryParse($targetVer, [ref]$verObj)) {
    throw "Invalid version format: $targetVer (expected e.g. 1.0.8)"
  }

  $before = Read-CsprojVersion $clientCsproj
  if ($before -ne $targetVer) {
    Set-CsprojVersion -csprojPath $clientCsproj -ver $targetVer
    Write-Log "CSProj version updated: $before -> $targetVer"
  } else {
    Write-Log "CSProj version already: $targetVer"
  }

  Run-DotnetPublish $UpdaterProjDir
  Run-DotnetPublish $ClientProjDir

  $clientPub  = Get-PublishDir $ClientProjDir
  $updaterPub = Get-PublishDir $UpdaterProjDir

  $stage = Join-Path $env:TEMP ("LanChatPkg_" + [guid]::NewGuid().ToString("N"))
  New-Item -ItemType Directory -Path $stage | Out-Null

  try {
    Copy-Item "$clientPub\*"  $stage -Recurse -Force
    Copy-Item "$updaterPub\LanChatUpdater.*" $stage -Force

    $zipPath = Join-Path $UpdatesDir "LanChatClient.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zipPath -Force
    $sha = (Get-FileHash $zipPath -Algorithm SHA256).Hash

    # 1) VERIFY ZIP FIRST (avoid half-publish)
    $checkDir = Join-Path $env:TEMP ("ZipCheck_" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $checkDir | Out-Null
    try {
      Expand-Archive -Path $zipPath -DestinationPath $checkDir -Force
      $exeInZip = Join-Path $checkDir $ExeName
      $pvNorm = Get-ExeProductVersionNormalized $exeInZip
      if ($pvNorm -ne $targetVer) {
        throw "MISMATCH: target=$targetVer but EXE in ZIP ProductVersion(normalized)=$pvNorm. Aborting publish."
      }
    }
    finally {
      if (Test-Path $checkDir) { Remove-Item $checkDir -Recurse -Force }
    }

    # 2) WRITE version.json AFTER successful verification
    $versionJsonPath = Join-Path $UpdatesDir "version.json"
    $json = @{
      version = $targetVer
      zipUrl  = ($BaseUpdatesUrl.TrimEnd('/') + "/LanChatClient.zip")
      sha256  = $sha
      exeName = $ExeName
    } | ConvertTo-Json -Depth 3

    Set-Content -Path $versionJsonPath -Value $json -Encoding UTF8

    $len = (Get-Item $zipPath).Length
    Write-Log "PUBLISH OK: version=$targetVer sha256=$sha zipBytes=$len zip=$zipPath"
    Write-Host ""
    Write-Host "OK. Updated:"
    Write-Host " - $zipPath"
    Write-Host " - $versionJsonPath"
    Write-Host "SHA256=$sha"
    Write-Host ""

    $changelog = Join-Path $UpdatesDir "CHANGELOG.md"
    if (!(Test-Path $changelog)) {
      Set-Content -Path $changelog -Value "# LanChat - Changelog`r`n" -Encoding UTF8
    }
    $add = Read-Host "Append entry to CHANGELOG.md (header + date)? [Y/N]"
    if (($add ?? "").Trim().ToUpperInvariant() -eq "Y") {
      $d = (Get-Date).ToString("yyyy-MM-dd")
      Add-Content -Path $changelog -Value "`r`n## $d - $targetVer`r`n- (fill in changes)`r`n" -Encoding UTF8
      Write-Log "CHANGELOG appended for version=$targetVer"
    }

  }
  finally {
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
  }

}
finally {
  if ($lockStream) { $lockStream.Dispose() }
}

# FILE_VERSION_END: Publish-LanChat.ps1 v1.1 (2026-02-20)