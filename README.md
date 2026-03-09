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


## Auto Update

LanChat posiada wbudowany system aktualizacji.

Mechanizm:

1. klient pobiera version.json
2. sprawdza SHA256
3. pobiera ZIP
4. uruchamia updater
5. updater wykonuje atomic swap katalogów
6. uruchamia się nowa wersja klienta

Update jest odporny na:
- blokady plików
- częściowe instalacje
- brak runtime .NET

