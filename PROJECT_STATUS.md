# FILE_VERSION: PROJECT_STATUS.md v1.1 (2026-02-22)

# LanChat – Project Status

## 1. Aktualna wersja produkcyjna

Wersja produkcyjna: **1.0.10**

- Publish wykonywany przez: `Publish-LanChat.ps1 v1.1`
- Auto-update działa (SHA256 + walidacja wersji EXE)
- Klienci aktualizują się poprawnie przy starcie
- Instalator (bootstrap) działa na 1 komputerze testowym

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
- dokumentacja

Repo NIE zawiera:
- runtime
- ZIP-ów
- SQLite
- logów

---

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
- `/updates` (z runtime, nie z repo)

---

## 3. Kluczowe pliki i ścieżki

- Serwer: `C:\LanChat\src\LanChatServer\Program.cs`
  - `/updates` mapowany na: `C:\LanChat\runtime\server\Updates`
  - SQLite na: `C:\LanChat\runtime\server\Data\lanchat.db`
- Publish: `C:\LanChat\src\LanChatServer\tools\Publish-LanChat.ps1` (v1.1)
- Installer script: `C:\LanChat\src\installer\LanChatSetup.iss` (v1.2)
- Output instalatora: `C:\LanChat\src\installer\Output\LanChat_Setup.exe`

---

## 4. Proces wydania (Release flow)

1. Zmiana kodu
2. `git commit`
3. `git push`
4. Uruchomienie publish:
   `pwsh -NoProfile -ExecutionPolicy Bypass -File "C:\LanChat\src\LanChatServer\tools\Publish-LanChat.ps1"`
5. (Po publish) Commit zmian wersji w repo (skrypt zmienia `LanChatClient.csproj`)
6. (Opcjonalnie) Budowa instalatora:
   `ISCC.exe C:\LanChat\src\installer\LanChatSetup.iss`
7. Klienci aktualizują się automatycznie

---

## 5. Stabilne elementy systemu

- SignalR chat
- Obsługa offline + kolejka
- SQLite persistence
- Auto-update z walidacją SHA
- Lockfile publish
- Oddzielenie runtime od repo
- Wersjonowanie w csproj
- Instalator bootstrap (Inno Setup)

---

## 6. Aktualne decyzje architektoniczne

- Runtime jest oddzielony od repo
- `version.json` znajduje się w runtime
- SQLite znajduje się w runtime
- ZIP produkcyjny nie jest wersjonowany w Git
- Serwer obecnie uruchamiany przez `dotnet run` (tryb dev)
- Autostart instalatora: machine-wide (HKLM Run)
- Skrót na pulpit: common (dla wszystkich użytkowników)

---

## 7. Rzeczy do zrobienia (najbliższe)

1. Bug: po kliknięciu w powiadomienie wiadomość widoczna x2 (do analizy eventów/handlerów)
2. UI: dodać więcej kolorów / paletę i spójne style XAML
3. Funkcja: wysyłanie plików (preferowane HTTP upload + link w czacie)

---

## 8. Rzeczy do rozważenia / planowane

- Publish serwera (oddzielenie od dotnet run)
- Uruchamianie serwera jako Windows Service
- Mechanizm anty-pętli update
- Kanał beta / stable
- Automatyczny workflow release (opcjonalnie GitHub Actions)

---

## 9. Informacja dla nowego wątku AI

Ten projekt:
- Ma oddzielone repo i runtime
- Auto-update jest produkcyjny i działa
- Publish działa
- Instalator bootstrap działa
- Aktualna wersja produkcyjna: **1.0.10**

Nowy wątek powinien:
1. Przeczytać `PROJECT_STATUS.md` i `README.md`
2. Nie proponować cofnięcia separacji runtime/repo
3. Zacząć od punktów z sekcji “Rzeczy do zrobienia”

---

# FILE_VERSION_END: PROJECT_STATUS.md v1.1 (2026-02-22)