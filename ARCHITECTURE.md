# FILE_VERSION: ARCHITECTURE.md v1.0 (2026-03-14)

LanChat – Architecture

====================================================================

OVERVIEW

LanChat jest wewnętrznym komunikatorem LAN działającym w sieci lokalnej.

System składa się z czterech głównych komponentów:

1. LanChatServer
2. LanChatClient
3. LanChatUpdater
4. Installer (Inno Setup)

====================================================================

REPOSITORY STRUCTURE

Repozytorium projektu znajduje się w:

C:\LanChat\src

Główne katalogi:

C:\LanChat\src\LanChatServer
C:\LanChat\src\LanChatClient
C:\LanChat\src\installer

====================================================================

RUNTIME STRUCTURE

Środowisko produkcyjne znajduje się w:

C:\LanChat\runtime

Najważniejsze katalogi:

C:\LanChat\runtime\server
C:\LanChat\runtime\server\app
C:\LanChat\runtime\server\Updates
C:\LanChat\runtime\server\Data

====================================================================

SERVER

Projekt:

LanChatServer

Technologia:

.NET 8
ASP.NET Core
SignalR

Serwer działa jako:

Windows Service

Nazwa usługi:

LanChatServer

Binarka runtime:

C:\LanChat\runtime\server\app\LanChatServer.exe

Serwer nasłuchuje na:

http://0.0.0.0:5001

Endpoint testowy:

http://127.0.0.1:5001/

Powinien zwracać:

LanChatServer OK

====================================================================

DATABASE

Baza danych:

SQLite

Lokalizacja:

C:\LanChat\runtime\server\Data\lanchat.db

Najważniejsza tabela:

Users

Pola:

User
Machine
IsOnline
LastSeenUtc
IsArchived
ArchivedAtUtc

====================================================================

USER ARCHIVING

Jeżeli użytkownik nie był aktywny przez ponad 3 dni robocze:

IsArchived = 1

Kontakt znika z listy w kliencie.

Rekord nie jest kasowany z bazy.

Po ponownym logowaniu:

IsArchived = 0

====================================================================

CLIENT

Projekt:

LanChatClient

Technologia:

.NET 8
WPF

Klient komunikuje się z serwerem przez:

SignalR

====================================================================

AUTO UPDATE

Klient posiada wbudowany system aktualizacji.

Proces:

1. klient pobiera version.json
2. porównuje wersję
3. pobiera LanChatClient.zip
4. uruchamia LanChatUpdater.exe
5. updater wykonuje aktualizację

Pliki aktualizacji znajdują się w:

C:\LanChat\runtime\server\Updates

====================================================================

UPDATER

Projekt:

LanChatUpdater

Updater odpowiada za:

zamknięcie klienta
rozpakowanie ZIP
atomic swap katalogów
restart klienta

Updater jest budowany jako:

self-contained single-file

Plik:

LanChatUpdater.exe

====================================================================

INSTALLER

Instalator budowany jest przez:

C:\LanChat\src\installer\Build-Installer.ps1

Output:

C:\LanChat\src\installer\Output\LanChat_Setup.exe

Installer używa:

Inno Setup

====================================================================

INSTALLER BUILD

Build tworzy katalog staging:

C:\LanChat\src\installer\build\client
C:\LanChat\src\installer\build\updater

Zasada:

client → self-contained NON single-file
updater → self-contained SINGLE file

Instalator kopiuje:

cały katalog client
tylko LanChatUpdater.exe

Zapobiega to nadpisywaniu DLL klienta przez publish updatera.

====================================================================

DEPLOY WORKFLOW

Standardowa kolejność wdrożenia:

1. Backup
2. git commit
3. git push
4. Deploy server
5. Publish client
6. Build installer

====================================================================

SERVER DEPLOY

Build:

cd C:\LanChat\src\LanChatServer
dotnet build

Deploy:

pwsh -NoProfile -ExecutionPolicy Bypass -File "C:\LanChat\src\LanChatServer\tools\Deploy-LanChatServer.ps1"

====================================================================

CLIENT PUBLISH

pwsh -NoProfile -ExecutionPolicy Bypass -File "C:\LanChat\src\LanChatServer\tools\Publish-LanChat.ps1"

Tworzone pliki:

LanChatClient.zip
version.json

====================================================================

INSTALLER BUILD

cd C:\LanChat\src\installer

pwsh -NoProfile -ExecutionPolicy Bypass -File "C:\LanChat\src\installer\Build-Installer.ps1"

====================================================================

BACKUP

Backup wykonywany przez:

C:\LanChat\src\LanChatServer\tools\Backup-LanChat.ps1

Backup zawiera:

src
runtime\server
MANIFEST.txt

====================================================================

LOG FILES

Update log:

C:\LanChat\log\update.log

Crash log:

C:\LanChat\log\crash.log

Updater log:

C:\ProgramData\LanChat\updater.log

====================================================================

ROADMAP

Planowane zmiany:

• poprawa zmiany nazwy użytkownika
• jedno okno rozmowy na kontakt
• powiadomienia taskbar
• wysyłanie obrazów
• restore serwera z backupu
• instalator dla innych organizacji

====================================================================

END OF DOCUMENT

# FILE_VERSION_END: ARCHITECTURE.md v1.0