# FILE_VERSION: README.md v1.7 (2026-02-27)

# LanChat

Wewnętrzny komunikator LAN.

------------------------------------------------------------------------

## Architektura

Repo: C:`\LanChat`{=tex}`\src  `{=tex} Runtime:
C:`\LanChat`{=tex}`\runtime`{=tex}`\server  `{=tex} Backup:
C:`\LanChatBackup  `{=tex}

------------------------------------------------------------------------

## Workflow pracy

### DEV

Serwer: cd /d C:`\LanChat`{=tex}`\src`{=tex}`\LanChatServer`{=tex}
dotnet run

Klient: cd /d
C:`\LanChat`{=tex}`\src`{=tex}`\LanChatClient`{=tex}`\LanChatClient`{=tex}
dotnet run

------------------------------------------------------------------------

### Backup

pwsh -NoProfile -ExecutionPolicy Bypass -File
"C:`\LanChat`{=tex}`\src`{=tex}`\LanChatServer`{=tex}`\tools`{=tex}`\Backup`{=tex}-LanChat.ps1"

------------------------------------------------------------------------

### Deploy serwera

pwsh -NoProfile -ExecutionPolicy Bypass -File
"C:`\LanChat`{=tex}`\src`{=tex}`\LanChatServer`{=tex}`\tools`{=tex}`\Deploy`{=tex}-LanChatServer.ps1"

------------------------------------------------------------------------

### Publish klienta

pwsh -NoProfile -ExecutionPolicy Bypass -File
"C:`\LanChat`{=tex}`\src`{=tex}`\LanChatServer`{=tex}`\tools`{=tex}`\Publish`{=tex}-LanChat.ps1"

------------------------------------------------------------------------

### Build instalatora

"C:`\Users`{=tex}`\Tomek`{=tex}`\AppData`{=tex}`\Local`{=tex}`\Programs`{=tex}`\Inno `{=tex}Setup
6`\ISCC`{=tex}.exe"
"C:`\LanChat`{=tex}`\src`{=tex}`\installer`{=tex}`\LanChatSetup`{=tex}.iss"

Output:
C:`\LanChat`{=tex}`\src`{=tex}`\installer`{=tex}`\Output`{=tex}`\LanChat`{=tex}\_Setup.exe

------------------------------------------------------------------------

## Zasada

src → test → backup → commit → deploy → publish → installer → runtime

Runtime nigdy nie jest modyfikowany ręcznie.

# FILE_VERSION_END: README.md v1.7 (2026-02-27)
