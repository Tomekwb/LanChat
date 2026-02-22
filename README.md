# FILE_VERSION: README.md v1.0 (2026-02-20)

# LanChat

Wewnętrzny komunikator LAN dla środowiska firmowego / medycznego.

Technologia: - .NET 8 - WPF (klient) - ASP.NET Core + SignalR (serwer) -
SQLite (persistencja) - Własny mechanizm auto-update (ZIP + SHA256)

------------------------------------------------------------------------

# 1. Architektura

## Repo (source code)

C:`\LanChat`{=tex}`\src`{=tex}

Zawiera: - LanChatClient (WPF) - LanChatUpdater (konsola) -
LanChatServer (ASP.NET Core) - tools (Publish-LanChat.ps1) -
dokumentację

Repo nie zawiera: - runtime - plików ZIP - bazy SQLite - logów

------------------------------------------------------------------------

## Runtime (produkcja)

C:`\LanChat`{=tex}`\runtime`{=tex}`\server`{=tex}

Zawiera: - Updates\
- Data`\lanchat`{=tex}.db - logs\
- publish_log.txt - CHANGELOG.md

Serwer hostuje: - /chat (SignalR) - /updates (z runtime)

------------------------------------------------------------------------

# 2. Uruchomienie serwera (DEV)

``` bash
cd C:\LanChat\src\LanChatServer
dotnet run
```

Serwer nasłuchuje na: http://0.0.0.0:5001

Test: http://127.0.0.1:5001/

------------------------------------------------------------------------

# 3. Uruchomienie klienta (DEV)

``` bash
cd C:\LanChat\src\LanChatClient\LanChatClient
dotnet run
```

------------------------------------------------------------------------

# 4. Proces wydania (Release)

1.  Wprowadź zmiany w kodzie
2.  git commit
3.  git push
4.  Uruchom:

``` bash
pwsh -NoProfile -ExecutionPolicy Bypass -File "C:\LanChat\src\LanChatServer\tools\Publish-LanChat.ps1"
```

Skrypt: - podbija wersję - buduje klienta i updater - tworzy ZIP - liczy
SHA256 - aktualizuje version.json - waliduje wersję EXE - zapisuje log

Klienci aktualizują się automatycznie przy starcie.

------------------------------------------------------------------------

# 5. Wersjonowanie

Źródło wersji: LanChatClient.csproj

version.json w runtime musi być zgodny z wersją EXE w ZIP.

------------------------------------------------------------------------

# 6. Zasady

-   Nie commitujemy runtime
-   Nie commitujemy SQLite
-   Nie commitujemy ZIP
-   Każdy release musi przejść przez Publish-LanChat.ps1
-   Runtime i repo są oddzielone

------------------------------------------------------------------------

# 7. Aktualna wersja produkcyjna

Sprawdź w:
C:`\LanChat`{=tex}`\runtime`{=tex}`\server`{=tex}`\Updates`{=tex}`\version`{=tex}.json

------------------------------------------------------------------------

# 8. Projekt

LanChat jest: - lokalnym komunikatorem - z obsługą offline - z kolejką
wiadomości - z auto-update - z kontrolą integralności paczki (SHA256)

------------------------------------------------------------------------

# FILE_VERSION_END: README.md v1.0 (2026-02-20)
