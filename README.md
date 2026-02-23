# FILE_VERSION: README.md v1.2 (2026-02-23)

# LanChat

Wewnętrzny komunikator LAN dla środowiska firmowego / medycznego.

Technologia:
- .NET 8
- WPF (klient)
- ASP.NET Core + SignalR (serwer)
- SQLite (persistencja)
- Własny mechanizm auto-update (ZIP + SHA256)
- Instalator bootstrap (Inno Setup)

---

## 1. Architektura

### Repo (source code)
`C:\LanChat\src`

Zawiera:
- LanChatClient (WPF)
- LanChatUpdater (konsola)
- LanChatServer (ASP.NET Core)
- tools (Publish-LanChat.ps1)
- installer (LanChatSetup.iss)
- dokumentację

Repo nie zawiera:
- runtime
- plików ZIP
- bazy SQLite
- logów

### Runtime (produkcja)
`C:\LanChat\runtime\server`

Zawiera:
- Updates\
- Data\lanchat.db
- logs\
- publish_log.txt
- CHANGELOG.md

Serwer hostuje:
- `/chat` (SignalR)
- `/updates` (z runtime)

---

## 2. Uruchomienie serwera (DEV)

```bat
cd /d C:\LanChat\src\LanChatServer && dotnet run

# 3. Uruchomienie klienta (DEV)

Projekt klienta znajduje się w podkatalogu rozwiązania.

Ścieżka projektu: `C:\LanChat\src\LanChatClient\LanChatClient`

Uruchomienie w trybie developerskim:

``` bat
cd /d C:\LanChat\src\LanChatClient\LanChatClient && dotnet run
```

Uwaga: Nie uruchamiać klienta z poziomu `C:\LanChat\src\LanChatClient`,
ponieważ właściwy plik `.csproj` znajduje się poziom niżej.

------------------------------------------------------------------------

# FILE_VERSION_END: README.md v1.2 (2026-02-23)