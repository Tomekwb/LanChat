# FILE_VERSION: PROJECT_STATUS.md v1.6 (2026-02-27)

# LanChat -- Project Status

## 1. Aktualna wersja produkcyjna

### Klient

Wersja produkcyjna: **1.0.16.0**

-   Auto-update działa (ZIP + SHA256 + walidacja wersji EXE)
-   Aktualizacja przy starcie klienta
-   Reconnect po restarcie serwera działa poprawnie

Zasada AutoUpdate (DEV vs PROD): - DEBUG / dotnet run: WYŁĄCZONY -
RELEASE: WŁĄCZONY

------------------------------------------------------------------------

### Serwer

-   Windows Service: LanChatServer
-   Binarka:
    C:`\LanChat`{=tex}`\runtime`{=tex}`\server`{=tex}`\app`{=tex}`\LanChatServer`{=tex}.exe
-   Recovery włączone
-   Test HTTP: http://127.0.0.1:5001/

------------------------------------------------------------------------

## 2. Architektura

Repo: C:`\LanChat`{=tex}`\src  `{=tex} Runtime:
C:`\LanChat`{=tex}`\runtime`{=tex}`\server  `{=tex} Backup:
C:`\LanChatBackup  `{=tex}

------------------------------------------------------------------------

## 3. Model pracy

### Development

Serwer: cd /d C:`\LanChat`{=tex}`\src`{=tex}`\LanChatServer`{=tex}
dotnet run

Klient: cd /d
C:`\LanChat`{=tex}`\src`{=tex}`\LanChatClient`{=tex}`\LanChatClient`{=tex}
dotnet run

------------------------------------------------------------------------

## 4. Backup

pwsh -NoProfile -ExecutionPolicy Bypass -File
"C:`\LanChat`{=tex}`\src`{=tex}`\LanChatServer`{=tex}`\tools`{=tex}`\Backup`{=tex}-LanChat.ps1"

Backup obejmuje: - src (bez .git/bin/obj) -
runtime`\server `{=tex}(pełny) - MANIFEST.txt

------------------------------------------------------------------------

## 5. Deploy serwera

pwsh -NoProfile -ExecutionPolicy Bypass -File
"C:`\LanChat`{=tex}`\src`{=tex}`\LanChatServer`{=tex}`\tools`{=tex}`\Deploy`{=tex}-LanChatServer.ps1"

sc.exe query LanChatServer

------------------------------------------------------------------------

## 6. Publish klienta

pwsh -NoProfile -ExecutionPolicy Bypass -File
"C:`\LanChat`{=tex}`\src`{=tex}`\LanChatServer`{=tex}`\tools`{=tex}`\Publish`{=tex}-LanChat.ps1"

------------------------------------------------------------------------

## 7. Build instalatora

"C:`\Users`{=tex}`\Tomek`{=tex}`\AppData`{=tex}`\Local`{=tex}`\Programs`{=tex}`\Inno `{=tex}Setup
6`\ISCC`{=tex}.exe"
"C:`\LanChat`{=tex}`\src`{=tex}`\installer`{=tex}`\LanChatSetup`{=tex}.iss"

Output:
C:`\LanChat`{=tex}`\src`{=tex}`\installer`{=tex}`\Output`{=tex}`\LanChat`{=tex}\_Setup.exe

------------------------------------------------------------------------

## 8. Oficjalna kolejność release

1.  Backup
2.  git commit
3.  git push
4.  Deploy serwera
5.  Publish klienta
6.  Build instalatora

# FILE_VERSION_END: PROJECT_STATUS.md v1.6 (2026-02-27)
