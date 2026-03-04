# Build-Installer.ps1
# Buduje publish klienta + updatéra, potem buduje instalator Inno Setup
# Output: C:\LanChat\src\installer\Output\LanChat_Setup.exe

$ErrorActionPreference = "Stop"

$root = "C:\LanChat\src"
$clientProj = Join-Path $root "LanChatClient\LanChatClient\LanChatClient.csproj"
$updaterProj = Join-Path $root "LanChatClient\LanChatUpdater\LanChatUpdater.csproj"
$issFile = Join-Path $root "installer\LanChatSetup.iss"

$iscc = "C:\Users\Tomek\AppData\Local\Programs\Inno Setup 6\ISCC.exe"

if (!(Test-Path $iscc)) { throw "Nie znaleziono ISCC.exe: $iscc" }
if (!(Test-Path $clientProj)) { throw "Nie znaleziono projektu klienta: $clientProj" }
if (!(Test-Path $updaterProj)) { throw "Nie znaleziono projektu updatéra: $updaterProj" }
if (!(Test-Path $issFile)) { throw "Nie znaleziono pliku Inno Setup: $issFile" }

Write-Host "=== Publish CLIENT ==="
dotnet publish $clientProj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

Write-Host "=== Publish UPDATER ==="
dotnet publish $updaterProj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

Write-Host "=== Build INSTALLER (Inno Setup) ==="
& $iscc $issFile

$outExe = Join-Path $root "installer\Output\LanChat_Setup.exe"
if (!(Test-Path $outExe)) { throw "Nie znaleziono output instalatora: $outExe" }

Write-Host ""
Write-Host "OK: gotowy instalator:"
Write-Host $outExe