#define AppVersion "1.0.5"
[Setup]
AppId={{B62F7EA8-2E66-4619-BBCD-66AD17C7FD79}
AppName=AG 4O3A
AppVersion={#AppVersion}
AppPublisher=Eduardo Arraia - CT7ASY
AppPublisherURL=https://github.com/CT7ASY/Antena-Genius-4O3A
AppUpdatesURL=https://github.com/CT7ASY/Antena-Genius-4O3A/releases/latest
LicenseFile=LICENSE
DefaultDirName=C:\CT7ASY\AG 4O3A
UsePreviousAppDir=no
DefaultGroupName=AG 4O3A
PrivilegesRequired=admin
MinVersion=10.0
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
UninstallDisplayIcon={app}\AG4O3A.exe
SetupIconFile=AG4O3A.ico
OutputDir=dist
OutputBaseFilename=Instalar-AG-4O3A-{#AppVersion}
VersionInfoVersion=1.0.5.0
[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
[Files]
Source: "build\AG4O3A.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "AG4O3A.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "languages.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "LEIA-ME.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{userprograms}\AG 4O3A"; Filename: "{app}\AG4O3A.exe"; Check: NotTestInstall
Name: "{userdesktop}\AG 4O3A"; Filename: "{app}\AG4O3A.exe"; Check: NotTestInstall
[Run]
Filename: "{app}\AG4O3A.exe"; Description: "Abrir AG 4O3A"; Flags: postinstall nowait skipifsilent runasoriginaluser
[Code]
function NotTestInstall: Boolean;
begin
 Result := ExpandConstant('{param:TESTINSTALL|0}') <> '1';
end;
function PrepareToInstall(var NeedsRestart: Boolean): String;
var Release: Cardinal;
begin
 Result := '';
 if not (RegQueryDWordValue(HKLM32, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040)) then
  Result := 'Instale o Microsoft .NET Framework 4.8 e volte a executar este instalador.';
 if NotTestInstall and CheckForMutexes('Local\CT7ASY.AG4O3A') then
  Result := 'Feche o AG 4O3A (incluindo a versão PowerShell) antes de continuar.';
end;
