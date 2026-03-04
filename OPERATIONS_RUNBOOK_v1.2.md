# LanChat – OPERATIONS RUNBOOK v1.2
(2026-02-27)

---

# 1. WYKONANIE KOPII ZAPASOWEJ

Backup obejmuje:
- C:\LanChat\src
- C:\LanChat\runtime\server
- MANIFEST.txt (SHA256 + rozmiar + ścieżka)

Lokalizacja backupu:
C:\LanChatBackup\LanChat_BACKUP_YYYYMMDD_HHMMSS

## Komenda


pwsh -NoProfile -ExecutionPolicy Bypass -File "C:\LanChat\src\LanChatServer\tools\Backup-LanChat.ps1"


---

# 2. PUBLIKACJA NOWEGO SERWERA

Zakładamy:
- zmiany wykonane w C:\LanChat\src\LanChatServer
- przetestowane przez:


cd /d C:\LanChat\src\LanChatServer
dotnet run


## Komenda deploy


pwsh -NoProfile -ExecutionPolicy Bypass -File "C:\LanChat\src\LanChatServer\tools\Deploy-LanChatServer.ps1"


## Sprawdzenie stanu usługi


sc.exe query LanChatServer


Status powinien być: RUNNING

---

# 3. PUBLIKACJA NOWEGO KLIENTA

Zakładamy:
- zmiany w C:\LanChat\src\LanChatClient
- podniesiona wersja w .csproj

## Komenda publish


pwsh -NoProfile -ExecutionPolicy Bypass -File "C:\LanChat\src\LanChatServer\tools\Publish-LanChat.ps1"


Efekt:
- Tworzony ZIP
- Aktualizowany version.json
- Pliki trafiają do:


C:\LanChat\runtime\server\Updates


---

# STANDARDOWA KOLEJNOŚĆ WDROŻENIA


Backup

git commit

git push

Deploy serwera

Publish klienta


---

# KLUCZOWE ŚCIEŻKI

Repo (DEV):


C:\LanChat\src


Produkcja:


C:\LanChat\runtime\server


Backup:


C:\LanChatBackup


---
PUBLIKACJA NOWEGO INSTALATORA (Inno Setup)

Instalator masz tutaj:

C:\LanChat\src\installer\LanChatSetup.iss
1️⃣ Zbuduj nowy instalator

Komenda:

"C:\Users\Tomek\AppData\Local\Programs\Inno Setup 6\ISCC.exe" "C:\LanChat\src\installer\LanChatSetup.iss"


Output będzie w:

C:\LanChat\src\installer\Output\

Plik najczęściej:

LanChat_Setup.exe






Koniec dokumentu.