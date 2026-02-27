# FILE_VERSION: README.md v1.6 (2026-02-27)

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
- tools (Publish-LanChat.ps1, Deploy-LanChatServer.ps1, **Backup-LanChat.ps1**)
- installer (LanChatSetup.iss)
- dokumentację

Repo **NIE** zawiera:
- runtime
- plików ZIP produkcyjnych
- bazy SQLite
- logów
- uploadów

---

### Runtime (produkcja)
`C:\LanChat\runtime\server`

Zawiera:
- `Updates` (auto-update feed: ZIP + `version.json`)
- `Data\lanchat.db` (SQLite)
- `Files` (uploady)
- `logs\`
- `publish_log.txt`
- `CHANGELOG.md`
- `app\LanChatServer.exe` (binarka serwera – Windows Service)

Serwer hostuje:
- `/chat` (SignalR)
- `/updates` (z runtime)
- `/files` (static files + upload)

---

## 2. Model GitHub

Repozytorium: https://github.com/Tomekwb/LanChat.git  
Branch główny: `main`

### Co wersjonujemy
- kod klienta i serwera
- skrypty
- instalator
- dokumentację

### Czego NIE wersjonujemy
- `runtime/`
- SQLite DB
- logów
- uploadów
- binarek produkcyjnych

Runtime jest środowiskiem wykonawczym, nie częścią repo.

---

## 3. Standardowy workflow pracy

### Zmiany w kodzie
1. Pracujemy w: `C:\LanChat\src`
2. Testujemy lokalnie:

Serwer (DEV):
```bat
cd /d C:\LanChat\src\LanChatServer
dotnet run
```

Klient (DEV):
```bat
cd /d C:\LanChat\src\LanChatClient\LanChatClient
dotnet run
```

3. Jeśli działa:
```bat
git add .
git commit -m "opis zmian"
git push
```

---

## 4. AutoUpdate – zasady działania (DEV vs PROD)

AutoUpdate jest aktywny wyłącznie w buildzie **Release**:

- **DEBUG / `dotnet run`**: AutoUpdate **WYŁĄCZONY** (żeby DEV nie odpalał updatéra i nie uruchamiał innego EXE).
- **RELEASE / produkcja**: AutoUpdate **WŁĄCZONY**.

Implementacja (w `MainWindow.xaml.cs`):

```csharp
#if DEBUG
private static readonly bool EnableAutoUpdate = false;
#else
private static readonly bool EnableAutoUpdate = true;
#endif
```

---

## 5. Backup – JEDEN standard (żeby nie było chaosu)

**Jedyna lokalizacja backupów:**  
`C:\LanChatBackup\LanChat_BACKUP_YYYYMMDD_HHMMSS\`

Backup zawsze obejmuje:
- snapshot repo: `C:\LanChat\src` (bez `.git`, `bin`, `obj`)
- snapshot produkcji: `C:\LanChat\runtime\server` (w całości: Data/Files/Updates/logs/app)
- `MANIFEST.txt` z SHA256 + rozmiar + ścieżka względna

Skrypt backupu (w repo):
`C:\LanChat\src\LanChatServer\tools\Backup-LanChat.ps1`

Uruchomienie:
```bat
pwsh -NoProfile -ExecutionPolicy Bypass -File "C:\LanChat\src\LanChatServer\tools\Backup-LanChat.ps1"
```

Uwaga: istniejące stare backupy ZIP (np. `LanChatClient.zip`, `LanChatServer.zip`) mogą mieć historyczne ścieżki (sprzed rozdzielenia src/runtime).
Nowy standard to **mirror src + mirror runtime\server** w jednym katalogu.

---

## 6. Deploy klienta

Deploy przez: `Publish-LanChat.ps1`

Skrypt:
- buduje klienta
- tworzy ZIP
- aktualizuje `version.json`
- zapisuje do: `C:\LanChat\runtime\server\Updates`

Klienci aktualizują się automatycznie przy starcie (w produkcji).

---

## 7. Deploy serwera

Serwer działa jako Windows Service: `LanChatServer`

Binarka produkcyjna:
`C:\LanChat\runtime\server\app\LanChatServer.exe`

Deploy wykonujemy przez: `Deploy-LanChatServer.ps1`

Skrypt:
- zatrzymuje usługę
- robi backup EXE (w runtime)
- wykonuje `dotnet publish`
- kopiuje do runtime
- uruchamia usługę
- wykonuje test HTTP

Sterowanie usługą:
```bat
sc.exe start LanChatServer
sc.exe stop LanChatServer
sc.exe query LanChatServer
```

---

## 8. Zasada: src → runtime

- `src` = development + git
- `runtime` = produkcja (dane + feed + uploady + binarka)

Nigdy nie testujemy zmian bezpośrednio w runtime.
Najpierw test w `src`, potem publish/deploy.

---

# FILE_VERSION_END: README.md v1.6 (2026-02-27)
