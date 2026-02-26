# FILE_VERSION: README.md v1.3 (2026-02-26)

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
- plików ZIP produkcyjnych
- bazy SQLite
- logów

### Runtime (produkcja)
`C:\LanChat\runtime\server`

Zawiera:
- `Updates\` (auto-update feed: ZIP + version.json)
- `Data\lanchat.db` (SQLite)
- `Files\` (uploady)
- `logs\`
- `publish_log.txt`
- `CHANGELOG.md`
- `app\LanChatServer.exe` (binarka serwera uruchamiana jako usługa)

Serwer hostuje:
- `/chat` (SignalR)
- `/updates` (z runtime)
- `/files` (static files + upload)

---

## 2. Uruchomienie serwera

### Produkcja (zalecane): Windows Service
Binarka:
- `C:\LanChat\runtime\server\app\LanChatServer.exe`

Sterowanie:
```bat
sc.exe start "LanChatServer"
sc.exe stop "LanChatServer"
sc.exe query "LanChatServer"