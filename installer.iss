; Inno Setup Script for Measuring Stick
; Download Inno Setup from: https://jrsoftware.org/isinfo.php

#define MyAppName "Measuring Stick"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "CurioCrafter"
#define MyAppURL "https://github.com/CurioCrafter/MeasuringStick"
#define MyAppExeName "MeasuringStick.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
OutputDir=installer
OutputBaseFilename=MeasuringStick-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Uninstall previous version before installing
CloseApplications=yes
CloseApplicationsFilter=*.exe
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start with Windows"; GroupDescription: "Startup:"

[Files]
Source: "release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[InstallDelete]
; Clean up old files before installing new version
Type: files; Name: "{app}\{#MyAppExeName}"
Type: files; Name: "{app}\*.pdb"

[UninstallDelete]
; Clean up on uninstall
Type: files; Name: "{app}\*"
Type: dirifempty; Name: "{app}"

[UninstallRun]
; Kill the app before uninstalling
Filename: "taskkill"; Parameters: "/F /IM {#MyAppExeName}"; Flags: runhidden; RunOnceId: "KillApp"

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Kill running instance before install
  Exec('taskkill', '/F /IM MeasuringStick.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    // Kill running instance before copying files
    Exec('taskkill', '/F /IM MeasuringStick.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;
