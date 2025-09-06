#define MyAppName "DBF Tools"
#define MyAppPublisher "Morten Sparding"
#define MyAppURL "https://github.com/mortensp/DBF"
#define MyAppExeName "DBF.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{911BF21C-D8B4-41A6-B83D-D4E3690193B6}
AppName={#MyAppName}
AppVersion={#GetVersionNumbersString('D:\Build\DBF\publish\DBF.exe')}
VersionInfoVersion={#GetVersionNumbersString('D:\Build\DBF\publish\DBF.exe')}
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
[Setup]
ArchitecturesAllowed=x64compatible x86compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=D:\Build\DBF\Installer
OutputBaseFilename=DBF Setup
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: "D:\Build\DBF\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
;Source: "AccessDatabaseEngine_2010_x86.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\AccessDatabaseEngine_2010_x86.exe"; Parameters: "/quiet /norestart /passive"; StatusMsg: "Installerer Access Database Engine 2010..."; Flags: runhidden skipifdoesntexist
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsACEInstalled(): Boolean;
begin
  Result := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Office\14.0\Access Connectivity Engine');
end;

function InitializeSetup(): Boolean;
begin
  if IsACEInstalled() then
    Log('ACE 2010 already installed.')
  else
    Log('ACE 2010 not found. Will install.');
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then begin
    if not IsACEInstalled() then begin
      Log('Running ACE installer...');
      ShellExec('', ExpandConstant('{tmp}\AccessDatabaseEngine_2010_x86.exe'), '/quiet /norestart /passive', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end else begin
      Log('Skipping ACE installer, already present.');
    end;
  end;
end;