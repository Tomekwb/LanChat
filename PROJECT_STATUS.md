# FILE_VERSION: PROJECT_STATUS.md v1.4 (2026-02-26)

# LanChat – Project Status

---

## 1. Aktualna wersja produkcyjna

### Klient
Wersja produkcyjna: **1.0.16.0**

- Auto-update działa (ZIP + SHA256 + walidacja wersji EXE)
- Aktualizacja przy starcie klienta
- Reconnect po restarcie serwera działa poprawnie

**Zasada AutoUpdate (DEV vs PROD):**
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

### Serwer
- Uruchamiany jako **Windows Service**
- Nazwa usługi: `LanChatServer`
- Binarka produkcyjna: `C:\LanChat\runtime\server\app\LanChatServer.exe`
- Recovery włączone (auto-restart po awarii)
- Test HTTP lokalny: `http://127.0.0.1:5001/`

---

## 2. Architektura (stan obowiązujący)

### Repo (development)
`C:\LanChat\src`

Zawiera:
- LanChatClient (WPF)
- LanChatServer (ASP.NET Core + SignalR)
- LanChatUpdater
- tools (Publish-LanChat.ps1, Deploy-LanChatServer.ps1)
- installer
- dokumentację

Repo **NIE** zawiera:
- runtime
- SQLite
- uploadów
- ZIP produkcyjnych
- logów

---

### Runtime (produkcja)
`C:\LanChat\runtime\server`

Zawiera:
- `Updates\`
- `Data\lanchat.db`
- `Files\`
- `logs\`
- `publish_log.txt`
- `CHANGELOG.md`
- `app\LanChatServer.exe`

Runtime jest środowiskiem wykonawczym. **Nie podlega wersjonowaniu w Git.**

---

## 3. Model pracy

### Development
Zmiany wprowadzamy wyłącznie w: `C:\LanChat\src`

Testy lokalne:

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

---

### Deploy klienta
Skrypt: `Publish-LanChat.ps1`

Efekt:
- budowa klienta
- ZIP
- `version.json`
- zapis do: `C:\LanChat\runtime\server\Updates`

---

### Deploy serwera
Skrypt: `Deploy-LanChatServer.ps1`

Efekt:
- stop usługi
- backup EXE
- `dotnet publish`
- kopia do `runtime\server\app`
- start usługi
- smoke test HTTP

---

## 4. Stabilne elementy systemu

- SignalR chat
- Durable messages (offline queue)
- SQLite persistence
- Auto-update klienta
- Wysyłanie plików
- Serwer jako Windows Service
- Recovery po crashu
- Oddzielenie src od runtime

---

## 5. Zamknięte tematy

- Reconnect po restarcie serwera
- Bug podwójnej wiadomości po kliknięciu w powiadomienie

---

## 6. Aktualny backlog

1. Status „ktoś pisze…”
2. UI theme / palety kolorów
3. Rozszerzone zarządzanie historią
4. Uporządkowanie logowania serwera

---

## 7. Zasada nadrzędna

`src → test → commit → push → deploy → runtime`

Nigdy:
- nie edytujemy runtime ręcznie
- nie testujemy nowych zmian bezpośrednio na usłudze produkcyjnej
- nie commitujemy runtime do Git

---

# FILE_VERSION_END: PROJECT_STATUS.md v1.4 (2026-02-26)
