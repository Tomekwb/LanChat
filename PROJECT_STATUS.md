# FILE_VERSION: PROJECT_STATUS.md v1.2 (2026-02-26)

# LanChat – Project Status

## 1. Aktualna wersja produkcyjna

Aktualna wersja klienta (produkcyjna): **1.0.16.0**

Status:
- Publish wykonywany przez: `Publish-LanChat.ps1`
- Auto-update działa (ZIP + SHA256 + walidacja wersji EXE)
- Klienci aktualizują się poprawnie przy starcie
- Wysyłanie plików działa (test ~39 MB)
- Serwer działa jako **Windows Service** (SCM)

DONE:
- Enter = send, Shift+Enter = new line
- Kasowanie historii (przycisk działa)
- Chmurki UI (wstępnie)
- Wysyłanie plików

ZAMKNIĘTE:
- Reconnect po restarcie serwera: klienci łączą się ponownie poprawnie (po wypchnięciu aktualnej wersji na stacje)

---

## 2. Architektura (aktualny model)

### Repo (source code)
`C:\LanChat\src`

Zawiera:
- LanChatClient (WPF)
- LanChatUpdater (konsola)
- LanChatServer (ASP.NET Core + SignalR)
- tools (Publish-LanChat.ps1)
- installer (LanChatSetup.iss)
- dokumentację

Repo NIE zawiera:
- runtime
- ZIP-ów produkcyjnych
- SQLite (DB)
- logów

---

### Runtime (produkcja)
`C:\LanChat\runtime\server`

Zawiera dane/artefakty runtime:
- `Updates\`  (feed auto-update: ZIP + version.json)
- `Data\lanchat.db` (SQLite)
- `Files\` (uploady)
- `logs\`
- `publish_log.txt`
- `CHANGELOG.md`

Dodatkowo binarka serwera (deploy):
- `C:\LanChat\runtime\server\app\LanChatServer.exe`

Serwer hostuje:
- `/chat` (SignalR)
- `/updates` (z runtime)
- `/files` (static files + upload)

---

## 3. Kluczowe pliki i ścieżki

### Serwer
- Kod źródłowy: `C:\LanChat\src\LanChatServer\Program.cs`
- Runtime root: `C:\LanChat\runtime\server`
  - `/updates` -> `C:\LanChat\runtime\server\Updates`
  - `/files`   -> `C:\LanChat\runtime\server\Files`
  - SQLite DB  -> `C:\LanChat\runtime\server\Data\lanchat.db`
- Binarka usługi (publish output):
  - `C:\LanChat\runtime\server\app\LanChatServer.exe`

### Publish / Installer
- Publish: `C:\LanChat\src\LanChatServer\tools\Publish-LanChat.ps1`
- Installer script: `C:\LanChat\src\installer\LanChatSetup.iss`
- Output instalatora: `C:\LanChat\src\installer\Output\LanChat_Setup.exe`

---

## 4. Uruchamianie serwera

### Produkcja: Windows Service (SCM) – AKTUALNE
Serwis: `LanChatServer`

Start/Stop:
- `sc.exe start "LanChatServer"`
- `sc.exe stop "LanChatServer"`
- `sc.exe query "LanChatServer"`

Binarka:
- `C:\LanChat\runtime\server\app\LanChatServer.exe`

Recovery (restart po awarii):
- `sc.exe failure "LanChatServer" reset= 86400 actions= restart/5000/restart/5000/restart/5000`
- `sc.exe failureflag "LanChatServer" 1`

Uwaga:
- Jeśli `curl http://192.168.64.233:5001/` nie działa ze stacji roboczej, typowo winny jest firewall (port TCP 5001).

### DEV (opcjonalnie)
- `cd /d C:\LanChat\src\LanChatServer && dotnet run`

---

## 5. Proces wydania (Release flow)

1. Zmiana kodu (repo)
2. `git commit`
3. `git push`
4. Publish klienta/update feed:
   `pwsh -NoProfile -ExecutionPolicy Bypass -File "C:\LanChat\src\LanChatServer\tools\Publish-LanChat.ps1"`
5. (Opcjonalnie) Budowa instalatora:
   `ISCC.exe C:\LanChat\src\installer\LanChatSetup.iss`
6. Klienci aktualizują się automatycznie przy starcie

Dla serwera (usługa):
- Zmiany w serwerze publikować do: `C:\LanChat\runtime\server\app`
- Restart usługi po wdrożeniu:
  - `sc.exe stop "LanChatServer"`
  - `sc.exe start "LanChatServer"`

---

## 6. Stabilne elementy systemu

- SignalR chat
- Obsługa offline + kolejka / durable messages
- SQLite persistence
- Auto-update z walidacją SHA
- Lockfile publish
- Oddzielenie runtime od repo
- Instalator bootstrap (Inno Setup)
- Serwer jako Windows Service

---

## 7. Rzeczy do zrobienia (najbliższe)

1. Bug: po kliknięciu w powiadomienie wiadomość widoczna x2 (do analizy eventów/handlerów)
2. UI: dodać więcej kolorów / paletę i spójne style XAML
3. Uspójnienie dokumentacji i ścieżek runtime/app (utrzymać aktualność)

---

## 8. Rzeczy do rozważenia / planowane

- Publish serwera w pełni zautomatyzowany (np. skrypt deploy + restart usługi)
- Logowanie serwera do pliku/EventLog
- Kanał beta / stable
- Automatyczny workflow release (opcjonalnie GitHub Actions)

---

## 9. Informacja dla nowego wątku AI

Ten projekt:
- Ma oddzielone repo i runtime
- Auto-update jest produkcyjny i działa
- Publish działa
- Instalator bootstrap działa
- Serwer działa jako Windows Service
- Runtime:
  - `C:\LanChat\runtime\server` (Data/Files/Updates/logs)
  - `C:\LanChat\runtime\server\app\LanChatServer.exe` (binarka serwera)

Nowy wątek powinien:
1. Przeczytać `PROJECT_STATUS.md` i `README.md`
2. Nie proponować cofnięcia separacji runtime/repo
3. Zacząć od punktów z sekcji “Rzeczy do zrobienia”

---

# FILE_VERSION_END: PROJECT_STATUS.md v1.2 (2026-02-26)