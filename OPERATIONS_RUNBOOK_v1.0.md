# FILE_VERSION: OPERATIONS_RUNBOOK.md v1.0 (2026-02-26)

# LanChat -- OPERATIONS / RUNBOOK

Dokument operacyjny do codziennej administracji systemem LanChat.

------------------------------------------------------------------------

## 1. Środowiska

### Development (repo)

C:`\LanChat`{=tex}`\src`{=tex}

### Produkcja (runtime)

C:`\LanChat`{=tex}`\runtime`{=tex}`\server`{=tex}

### Binarka serwera (usługa)

C:`\LanChat`{=tex}`\runtime`{=tex}`\server`{=tex}`\app`{=tex}`\LanChatServer`{=tex}.exe

Usługa Windows: LanChatServer

------------------------------------------------------------------------

## 2. Sprawdzenie czy serwer działa

Status usługi: sc.exe query LanChatServer

Test HTTP lokalny: curl.exe -s http://127.0.0.1:5001/

Test z innej stacji: Test-NetConnection 192.168.64.233 -Port 5001

------------------------------------------------------------------------

## 3. Restart serwera

    sc.exe stop LanChatServer
    sc.exe start LanChatServer

------------------------------------------------------------------------

## 4. Deploy serwera (po zmianach w kodzie)

1.  Zmiany w: C:`\LanChat`{=tex}`\src`{=tex}

2.  Test lokalny (DEV): cd
    C:`\LanChat`{=tex}`\src`{=tex}`\LanChatServer`{=tex} dotnet run

3.  Deploy produkcyjny: pwsh -NoProfile -ExecutionPolicy Bypass -File \`
    "C:`\LanChat`{=tex}`\src`{=tex}`\LanChatServer`{=tex}`\tools`{=tex}`\Deploy`{=tex}-LanChatServer.ps1"

Skrypt: - zatrzymuje usługę - robi backup EXE - wykonuje publish -
uruchamia usługę - robi test HTTP

------------------------------------------------------------------------

## 5. Deploy klienta

    pwsh -NoProfile -ExecutionPolicy Bypass -File `
    "C:\LanChat\src\LanChatServer\tools\Publish-LanChat.ps1"

Efekt: - ZIP + version.json - zapis do
runtime`\server`{=tex}`\Updates`{=tex} - klienci aktualizują się przy
starcie

------------------------------------------------------------------------

## 6. Backup bazy danych

Plik:
C:`\LanChat`{=tex}`\runtime`{=tex}`\server`{=tex}`\Data`{=tex}`\lanchat`{=tex}.db

Prosty backup ręczny: copy
C:`\LanChat`{=tex}`\runtime`{=tex}`\server`{=tex}`\Data`{=tex}`\lanchat`{=tex}.db
D:`\backup`{=tex}`\lanchat`{=tex}.db

Zalecenie: - backup dzienny - nie wykonywać kopiowania przy intensywnym
zapisie (najlepiej po stop usługi)

------------------------------------------------------------------------

## 7. Firewall (jeśli brak dostępu z LAN)

Sprawdzenie portu: Test-NetConnection 192.168.64.233 -Port 5001

Dodanie reguły (na serwerze): New-NetFirewallRule -DisplayName "LanChat
5001" \` -Direction Inbound -Protocol TCP -LocalPort 5001 -Action Allow

------------------------------------------------------------------------

## 8. Logika pracy (zasada nadrzędna)

src → test → commit → push → deploy → runtime

Nigdy: - nie edytujemy runtime ręcznie - nie testujemy zmian
bezpośrednio na usłudze produkcyjnej - nie commitujemy runtime do Git

------------------------------------------------------------------------

## 9. Najczęstsze problemy

Serwer nie startuje: - sprawdź sc.exe query - sprawdź czy port 5001 jest
wolny - sprawdź czy EXE istnieje w runtime`\server`{=tex}`\app`{=tex}

Klient nie widzi serwera: - sprawdź firewall - sprawdź
Test-NetConnection - sprawdź czy usługa RUNNING

Auto-update nie działa: - sprawdź
runtime`\server`{=tex}`\Updates`{=tex}`\version`{=tex}.json - sprawdź
czy klient ma EnableAutoUpdate = true

------------------------------------------------------------------------

# FILE_VERSION_END: OPERATIONS_RUNBOOK.md v1.0 (2026-02-26)
