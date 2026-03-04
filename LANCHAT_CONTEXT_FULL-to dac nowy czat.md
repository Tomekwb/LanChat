# LANCHAT -- FULL TECHNICAL CONTEXT (ARCHITECTURE & OPERATIONS)

## 1. Cel projektu

LanChat to wewnętrzny komunikator LAN oparty o:

-   .NET 8
-   WPF (klient)
-   ASP.NET Core + SignalR (serwer)
-   HTTP endpointy do:
    -   auto-update
    -   uploadu plików

Projekt przeznaczony do pracy w sieci lokalnej (192.168.64.x).

------------------------------------------------------------------------

## 2. Architektura wysokiego poziomu

\[ WPF Client \] \| \| SignalR (/chat) v \[ ASP.NET Core Server \] \| \|
HTTP: \| /updates \| /files/upload v \[ Storage / Update ZIP \]

Serwer pełni 3 role: 1. Hub komunikatora (SignalR) 2. Host aktualizacji
3. Endpoint uploadu plików

------------------------------------------------------------------------

## 3. Struktura projektu

Repozytorium: https://github.com/Tomekwb/LanChat.git

Lokalnie: C:`\LanChat`{=tex}`\src`{=tex}\
LanChatClient\
LanChatServer\

Runtime klienta (po instalacji): C:`\LanChat`{=tex}

------------------------------------------------------------------------

## 4. Endpointy i adresy

SignalR Hub: http://192.168.64.233:5001/chat

Serwer musi mieć: app.MapHub`<ChatHub>`{=html}("/chat");

Base URL: http://192.168.64.233:5001

Aktualizacje: /updates

Upload plików: POST /files/upload

------------------------------------------------------------------------

## 5. Klient -- MainWindow.xaml.cs (logika centralna)

Odpowiada za: - inicjalizację aplikacji - tray icon - unread queue -
blink timer - obsługę reconnect - wywołanie Register po starcie -
auto-update - upload plików - otwieranie okien rozmów - historię
wiadomości

Przepływ startu: 1. LoadOrAskName() 2. StartupAsync() 3.
CheckAndUpdateIfNeededAsync() 4. StartConnection() 5. Register(user,
machine)

------------------------------------------------------------------------

## 6. Auto-update -- mechanizm

Flaga: private const bool EnableAutoUpdate = true;

Na starcie: UpdateManager.CheckAndUpdateIfNeededAsync(UpdatesBaseUrl,
InstallDir)

Jeżeli updater się uruchomi -- aplikacja musi się zamknąć, aby nie
blokować plików w katalogu instalacyjnym.

------------------------------------------------------------------------

## 7. SignalR -- Flow komunikacji

Połączenie budowane przez:
HubConnectionBuilder().WithUrl(ServerHubUrl).WithAutomaticReconnect().Build();

Po StartAsync(): Register(user, machine)

Obsługiwane eventy: - OnlineUsers (lista obecnych) - PrivateMessage
(wiadomość prywatna)

Problem do rozwiązania: Po restarcie serwera konieczne jest ponowne
wywołanie Register w zdarzeniu Reconnected, ponieważ serwer traci stan
presence.

------------------------------------------------------------------------

## 8. Historia wiadomości

Metody: - GetHistory(user, limit) - DeleteHistory(user)

Klient utrzymuje: - \_historyLoaded - \_pending (bufor nieotwartych
rozmów)

------------------------------------------------------------------------

## 9. Wysyłanie plików

HTTP Multipart: POST /files/upload

Oczekiwana odpowiedź JSON: { "body": "LCFILE\|name\|size\|url" }

Klient wyświetla body jako wiadomość czatu.

------------------------------------------------------------------------

## 10. Backlog rozwoju

1.  Reconnect z pełnym re-register
2.  Status „ktoś pisze..."
3.  UI theme / kolory
4.  Rozszerzone zarządzanie historią
5.  Dalsza optymalizacja update pipeline

------------------------------------------------------------------------

## 11. Zasady pracy nad projektem

-   Kod w repo jest źródłem prawdy.
-   Ten plik jest dokumentacją architektury.
-   Przy zmianach w plikach .cs obowiązuje aktualnie wklejona wersja.
-   Pliki do \~1000 linii wklejane bezpośrednio w czacie.
-   Unikać trzymania pełnych plików kodu w dokumentacji.
