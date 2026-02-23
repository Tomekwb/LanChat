; FILE_VERSION: LanChatSetup.iss v1.2 (2026-02-22)
; Universal installer: shortcuts + autostart for ALL USERS (common desktop + HKLM Run)

[Setup]
AppName=LanChat
AppVersion=bootstrap
DefaultDirName=C:\LanChat
DisableDirPage=yes

; Start Menu folder for all users
DefaultGroupName=LanChat
PrivilegesRequired=admin

OutputBaseFilename=LanChat_Setup
Compression=lzma
SolidCompression=yes

ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"

[Files]
; Client publish (self-contained)
Source: "C:\LanChat\src\LanChatClient\LanChatClient\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Updater publish
Source: "C:\LanChat\src\LanChatClient\LanChatUpdater\bin\Release\net8.0-windows\win-x64\publish\LanChatUpdater.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\LanChat\src\LanChatClient\LanChatUpdater\bin\Release\net8.0-windows\win-x64\publish\LanChatUpdater.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\LanChat\src\LanChatClient\LanChatUpdater\bin\Release\net8.0-windows\win-x64\publish\LanChatUpdater.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

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

; FILE_VERSION_END: LanChatSetup.iss v1.2 (2026-02-22)