; FILE_VERSION: LanChatSetup.iss v1.3 (2026-02-27)
; Universal installer: shortcuts + autostart for ALL USERS (common desktop + HKLM Run)

#define Root "C:\LanChat"
#define Src  Root + "\src"

#define ClientPublish  Src + "\LanChatClient\LanChatClient\bin\Release\net8.0-windows\win-x64\publish"
#define UpdaterPublish Src + "\LanChatClient\LanChatUpdater\bin\Release\net8.0-windows\win-x64\publish"

#define ClientExe ClientPublish + "\LanChatClient.exe"
#define AppVer GetVersionNumbersString(ClientExe)

[Setup]
AppName=LanChat
AppVersion={#AppVer}
AppPublisher=LanChat
DefaultDirName={#Root}
DisableDirPage=yes

; Start Menu folder for all users
DefaultGroupName=LanChat
PrivilegesRequired=admin

OutputBaseFilename=LanChat_Setup
Compression=lzma
SolidCompression=yes

ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; (opcjonalnie) ikona w "Programy i funkcje"
UninstallDisplayIcon={app}\LanChatClient.exe

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"

[Files]
; Client publish (self-contained / publish output)
Source: "{#ClientPublish}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Updater publish (kopiujemy CAŁOŚĆ, żeby nie brakowało dll/deps/runtimeconfig)
Source: "{#UpdaterPublish}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu for all users
Name: "{commonprograms}\LanChat\LanChat"; Filename: "{app}\LanChatClient.exe"

; Desktop for all users
Name: "{commondesktop}\LanChat"; Filename: "{app}\LanChatClient.exe"

[Registry]
; Autostart for all users (machine-wide)
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "LanChat"; ValueData: """{app}\LanChatClient.exe"""; Flags: uninsdeletevalue

[Run]
Filename: "{app}\LanChatClient.exe"; Description: "Uruchom LanChat"; Flags: nowait postinstall skipifsilent

; FILE_VERSION_END: LanChatSetup.iss v1.3 (2026-02-27)