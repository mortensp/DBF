#define MyAppName "DBF Tools"
#define MyAppPublisher "Morten Sparding"
#define MyAppURL "https://github.com/mortensp/DBF"
#define MyAppExeName "DBF.exe"
#define MyVersion GetVersionNumbersString('D:\Build\DBF\publish\DBF.exe')
#pragma message "Aktuel Program Version: " + MyVersion

[Setup]
ArchitecturesInstallIn64BitMode=x64compatible
UsePreviousLanguage=no

AppId={{911BF21C-D8B4-41A6-B83D-D4E3690193B6}}
AppName={#MyAppName}
AppVersion={#MyVersion}
VersionInfoVersion={#MyVersion}
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\mortensp
UninstallDisplayIcon={#MyAppExeName}

ArchitecturesAllowed=x64compatible 
;ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir="D:\Build\DBF\Installer"
OutputBaseFilename="DBF Setup"
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "D:\Build\DBF\publish\*"; DestDir: "{app}\DBF"; Flags: ignoreversion recursesubdirs
Source: "D:\Build\Github Updater\publish\*"; DestDir: "{app}\Github"; Flags: ignoreversion recursesubdirs
Source: "D:\Build\Bootstrapper\publish\*"; DestDir: "{app}\Bootstrapper"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\DBF\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}";  Filename: "{app}\DBF\{#MyAppExeName}"; Tasks: desktopicon
;Name: "{uninstallicon}{#MyAppName}"; Filename: "{app}\DBF\{#MyAppExeName}"

[Run]
;Filename: "{app}\DBF\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
;Filename: "{app}\Bootstrapper\Bootstrapper.exe"; Description: "Starting the application"; Flags: nowait postinstall skipifsilent

[Code]
const
  DotNet9Url = 'https://download.visualstudio.microsoft.com/download/pr/.../windowsdesktop-runtime-9.0.0-win-x64.exe';
  
function IsDotNet9Installed(): Boolean;
begin
  Result := RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App');
end;

procedure InitializeWizard();
var
  TempFile: string;
  ResultCode: Integer;
begin
  if not IsDotNet9Installed() then
  begin
    TempFile := ExpandConstant('{tmp}\dotnet9desktop.exe');

    idpDownloadFile(DotNet9Url, TempFile);

    Exec(TempFile, '/quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
  end;
end;