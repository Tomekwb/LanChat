# LanChat – jak to działa (serwer, klient, auto-update)

Data: 2026-02-19

## 0. TL;DR (co robisz na co dzień)
Publikacja nowej wersji = uruchamiasz 1 skrypt na serwerze:
- buduje klienta i updatera (dotnet publish)
- pakuje wszystko do ZIP
- liczy SHA256
- aktualizuje version.json
- dopisuje log publikacji
- sprawdza, czy ZIP zawiera EXE o tej samej wersji co version.json

Skrypt: `C:\LanChatServer\tools\Publish-LanChat.ps1`

---

## 1. Struktura katalogów (ważne)

### 1.1 Serwer (hostuje HTTP + SignalR)
- `C:\LanChatServer\` – projekt serwera (`dotnet run`)
- `C:\LanChatServer\Updates\` – publiczne pliki aktualizacji (statyczne pliki HTTP)
  - `C:\LanChatServer\Updates\LanChatClient.zip`  (pakiet aktualizacji)
  - `C:\LanChatServer\Updates\version.json`       (manifest aktualizacji)
  - `C:\LanChatServer\Updates\publish_log.txt`    (log publikacji)
  - `C:\LanChatServer\Updates\CHANGELOG.md`       (opis zmian)

Serwer wystawia:
- `http://192.168.64.233:5001/` → "LanChatServer OK"
- `http://192.168.64.233:5001/chat` → SignalR Hub (klient się łączy)
- `http://192.168.64.233:5001/updates/version.json`
- `http://192.168.64.233:5001/updates/LanChatClient.zip`

### 1.2 Repo/projekty buildowane na serwerze
- `C:\LanChatClient\LanChatClient\`  – klient WPF
- `C:\LanChatClient\LanChatUpdater\` – updater (konsolowy .NET)

---

## 2. Co jest w paczce (ZIP) i dlaczego

W `LanChatClient.zip` MUSZĄ być:
- `LanChatClient.exe` + wszystkie dll/zasoby z publish klienta
- `LanChatUpdater.exe` + pliki runtime updatera:
  - `LanChatUpdater.deps.json`
  - `LanChatUpdater.runtimeconfig.json`
  - `LanChatUpdater.dll` (czasem wymagane zależnie od uruchomienia)
  - opcjonalnie `.pdb` (debug)

Dlaczego:
- Klient pobiera ZIP do `%TEMP%\LanChatClient_update.zip`
- Klient uruchamia `LanChatUpdater.exe` z argumentami: zip, installDir, exeName
- Updater rozpakowuje ZIP do staging i kopiuje pliki do `C:\LanChat`

---

## 3. Jak działa auto-update (krok po kroku)

1) Klient na starcie pobiera:
   - `/updates/version.json`

2) Porównuje:
   - `info.version` (z JSON) vs `CurrentVersion` (z Assembly klienta)

3) Jeśli `remote > local`:
   - pobiera ZIP z `info.zipUrl` do:
     - `%TEMP%\LanChatClient_update.zip`

4) Klient weryfikuje SHA256 ZIP:
   - liczy SHA256 lokalnego ZIP
   - porównuje do `info.sha256`
   - mismatch = STOP (bez uruchamiania updatera)

5) Klient uruchamia:
   - `C:\LanChat\LanChatUpdater.exe "<tempZip>" "C:\LanChat" "LanChatClient.exe"`

6) Klient zamyka się (żeby odblokować pliki).

7) Updater:
   - ubija proces klienta jeśli żyje
   - rozpakowuje ZIP do folderu staging w TEMP
   - kopiuje pliki staging -> `C:\LanChat`
   - uruchamia `C:\LanChat\LanChatClient.exe`
   - sprząta staging i ZIP

---

## 4. Co robisz gdy coś nie działa

### 4.1 Pętla aktualizacji (otwiera-zamyka-otwiera…)
Najczęstsze powody:
- `version.json` wskazuje wersję wyższą, ale ZIP ma inną wersję EXE
- brak updatera w instalacji (albo uruchamia się w trybie wymagającym DLL)
- w ZIP jest stary klient, a manifest nowy

Diagnostyka na stacji:
- sprawdź wersję klienta:
  ```powershell
  (Get-Item "C:\LanChat\LanChatClient.exe").VersionInfo | Format-List FileVersion,ProductVersion
