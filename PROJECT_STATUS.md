# FILE_VERSION: PROJECT_STATUS.md v1.0 (2026-02-20)

# LanChat – Project Status

## 1. Aktualna wersja produkcyjna

Wersja produkcyjna: **1.0.9**

- Publish wykonywany przez: `Publish-LanChat.ps1 v1.1`
- Auto-update działa (SHA256 + walidacja wersji EXE)
- Klienci aktualizują się poprawnie przy starcie

---

## 2. Architektura (aktualny model)

### Repo (source code)
C:\LanChat\src

Zawiera:
- LanChatClient (WPF)
- LanChatUpdater (konsola)
- LanChatServer (ASP.NET Core + SignalR)
- tools (Publish-LanChat.ps1)
- dokumentacja

Repo NIE zawiera:
- runtime
- ZIP-ów
- SQLite
- logów

---

### Runtime (produkcja)

C:\LanChat\runtime\server

Zawiera:
- Updates\
- Data\lanchat.db
- logs\
- publish_log.txt
- CHANGELOG.md

Serwer hostuje:
- /chat (SignalR)
- /updates (z runtime, nie z repo)

---

## 3. Proces wydania (Release flow)

1. Zmiana kodu
2. git commit
3. git push
4. Uruchomienie:
   pwsh -File Publish-LanChat.ps1
5. Powstaje:
   - LanChatClient.zip
   - version.json
   - SHA256
6. Klienci aktualizują się automatycznie

---

## 4. Stabilne elementy systemu

- SignalR chat
- Obsługa offline + kolejka
- SQLite persistence
- Auto-update z walidacją SHA
- Lockfile publish
- Oddzielenie runtime od repo
- Wersjonowanie w csproj

---

## 5. Aktualne decyzje architektoniczne

- Runtime jest oddzielony od repo
- version.json znajduje się w runtime
- SQLite znajduje się w runtime
- ZIP produkcyjny nie jest wersjonowany w Git
- Serwer obecnie uruchamiany przez dotnet run (tryb dev)

---

## 6. Rzeczy do rozważenia / planowane

- Publish serwera (oddzielenie od dotnet run)
- Uruchamianie serwera jako Windows Service
- Mechanizm anty-pętli update
- Kanał beta / stable
- Automatyczny workflow release (opcjonalnie GitHub Actions)

---

## 7. Ważne zasady

- Nie commitujemy runtime
- Nie commitujemy SQLite
- Nie commitujemy ZIP
- Każdy release musi przejść przez Publish-LanChat.ps1
- Wersja w csproj musi być zgodna z version.json

---

## 8. Informacja dla nowego wątku AI

Ten projekt:
- Ma oddzielone repo i runtime
- Auto-update jest produkcyjny
- Publish działa
- Aktualna wersja produkcyjna: 1.0.9

Nowy wątek powinien:
1. Przeczytać PROJECT_STATUS.md
2. Sprawdzić aktualną wersję produkcyjną
3. Nie proponować cofnięcia separacji runtime/repo

---

# FILE_VERSION_END: PROJECT_STATUS.md v1.0 (2026-02-20)