; =============================================================================
; VeloForge - Inno Setup 6.x Installer Script
; App:       VeloForge v0.1.0-alpha
; Publisher: VeloForge Engineering
;
; SourceDir and OutputDir are passed in by build_installer.ps1 via:
;   /DSourceDir=<repo root>  /DOutputDir=<repo root>\dist\installer
; This makes the script portable — no hardcoded drive letters.
; =============================================================================

#define AppName      "VeloForge"
#define AppVersion   "0.1.0-alpha"
#define AppPublisher "VeloForge Engineering"
#define AppURL       "https://github.com/ochidesoim/pico"
#define AppExeName   "VeloForge.exe"

; =============================================================================
[Setup]
; =============================================================================
AppId={{A3F7B2C1-44D8-4E9A-B6F0-12345678ABCD}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases

DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes

OutputDir={#OutputDir}
OutputBaseFilename=VeloForge_Setup
SetupIconFile={#SourceDir}\assets\veloforge.ico
UninstallDisplayIcon={app}\{#AppExeName}

Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

MinVersion=10.0.19041

PrivilegesRequired=admin

WizardStyle=modern
DisableWelcomePage=no

ShowLanguageDialog=auto

LicenseFile={#SourceDir}\LICENSE.txt

DisableDirPage=no
AppendDefaultDirName=no

; =============================================================================
[Languages]
; =============================================================================
Name: "english"; MessagesFile: "compiler:Default.isl"

; =============================================================================
[Tasks]
; =============================================================================
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

; =============================================================================
[Files]
; =============================================================================
Source: "{#SourceDir}\dist\VeloForge.exe";               DestDir: "{app}";          Flags: ignoreversion
Source: "{#SourceDir}\dist\pipeline\*";                  DestDir: "{app}\pipeline"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceDir}\dist\bins\fTetWild.exe";           DestDir: "{app}\bins";     Flags: ignoreversion
Source: "{#SourceDir}\dist\bins\ccx.exe";                DestDir: "{app}\bins";     Flags: ignoreversion
Source: "{#SourceDir}\dist\bins\ccx2paraview.exe";       DestDir: "{app}\bins";     Flags: ignoreversion
Source: "{#SourceDir}\dist\bins\*.dll";                  DestDir: "{app}\bins";     Flags: ignoreversion
Source: "{#SourceDir}\configs\*";                        DestDir: "{app}\configs";  Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceDir}\dist\redist\vc_redist.x64.exe";    DestDir: "{tmp}";          Flags: deleteafterinstall

; =============================================================================
[Icons]
; =============================================================================
Name: "{group}\{#AppName}";             Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}";       Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{group}\Uninstall {#AppName}";   Filename: "{uninstallexe}"

; =============================================================================
[Registry]
; =============================================================================
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}\bins"; Check: NeedsAddPath(ExpandConstant('{app}\bins')); Flags: preservestringtype

; =============================================================================
[Run]
; =============================================================================
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

; =============================================================================
[UninstallDelete]
; =============================================================================
Type: filesandordirs; Name: "{app}\bins"
Type: filesandordirs; Name: "{app}\pipeline"
Type: filesandordirs; Name: "{app}\configs"

; =============================================================================
[CustomMessages]
; =============================================================================
english.WelcomeLabel2=This wizard will install [name/ver] on your computer.%n%nThis is a computational engineering tool. All simulation outputs are design aids only. Independent validation by a qualified engineer is required before any physical use.%n%nClick Next to continue.

; =============================================================================
[Code]

// ------------------------------------------------------------------
// NeedsAddPath
// Returns True if PathToAdd is NOT already in system PATH.
// ------------------------------------------------------------------
function NeedsAddPath(PathToAdd: string): Boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(
    HKLM,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath)
  then begin
    Result := True;
    exit;
  end;
  Result := Pos(';' + Uppercase(PathToAdd) + ';',
                ';' + Uppercase(OrigPath)  + ';') = 0;
end;

// ------------------------------------------------------------------
// CheckDiskSpace
// Returns True if the target drive has > 2048 MB free.
// Uses drive root (e.g. C:\) — install dir may not exist yet.
// ------------------------------------------------------------------
function CheckDiskSpace(): Boolean;
var
  FreeBytes, TotalBytes: Cardinal;
  DriveRoot: string;
begin
  DriveRoot := ExtractFileDrive(WizardDirValue()) + chr(92);
  if not GetSpaceOnDisk(DriveRoot, True, FreeBytes, TotalBytes) then
  begin
    Result := True;
    exit;
  end;
  Result := FreeBytes > 2048;
end;

// ------------------------------------------------------------------
// RemovePathEntry
// Removes PathToRemove from the system PATH registry value.
// ------------------------------------------------------------------
procedure RemovePathEntry(PathToRemove: string);
var
  RegKey, OldPath, NewPath: string;
  Segments: TStringList;
  i: Integer;
begin
  RegKey := 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment';
  if not RegQueryStringValue(HKLM, RegKey, 'Path', OldPath) then exit;

  Segments := TStringList.Create;
  try
    Segments.Delimiter       := ';';
    Segments.StrictDelimiter := True;
    Segments.DelimitedText   := OldPath;

    for i := Segments.Count - 1 downto 0 do
    begin
      if Uppercase(Trim(Segments[i])) = Uppercase(Trim(PathToRemove)) then
        Segments.Delete(i);
    end;

    NewPath := Segments.DelimitedText;
  finally
    Segments.Free;
  end;

  RegWriteExpandStringValue(HKLM, RegKey, 'Path', NewPath);
end;

// ------------------------------------------------------------------
// VCRedistNeedsInstall
// Returns True if VC++ 2015-2022 x64 Redist is NOT installed.
// ------------------------------------------------------------------
function VCRedistNeedsInstall(): Boolean;
var
  SubKey: string;
  Installed: Cardinal;
begin
  SubKey := 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64';
  Result := not (RegQueryDWordValue(HKLM, SubKey, 'Installed', Installed)
                 and (Installed = 1));
end;

// ------------------------------------------------------------------
// NextButtonClick
// Rejects path traversal and enforces minimum 2 GB disk space.
// ------------------------------------------------------------------
function NextButtonClick(CurPageID: Integer): Boolean;
var
  Dir: string;
begin
  Result := True;
  if CurPageID = wpSelectDir then
  begin
    Dir := WizardDirValue();
    if (Pos('..', Dir) > 0) or (Pos('/', Dir) > 0) then
    begin
      MsgBox('Invalid installation path. Please choose a standard directory.',
             mbError, MB_OK);
      Result := False;
      exit;
    end;
    if not CheckDiskSpace() then
    begin
      MsgBox('Not enough disk space on the selected drive.' + #13#10 +
             'VeloForge requires at least 2 GB free.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

// ------------------------------------------------------------------
// InitializeSetup
// ------------------------------------------------------------------
function InitializeSetup(): Boolean;
begin
  Result := True;
end;

// ------------------------------------------------------------------
// CurStepChanged
// Installs VC++ Redistributable silently before main file copy.
// ------------------------------------------------------------------
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    if VCRedistNeedsInstall() then
    begin
      ExtractTemporaryFile('vc_redist.x64.exe');
      if not Exec(ExpandConstant('{tmp}\vc_redist.x64.exe'),
                  '/quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated,
                  ResultCode) then
      begin
        MsgBox('Failed to install VC++ Redistributable (error ' +
               IntToStr(ResultCode) + '). Installation may be incomplete.',
               mbError, MB_OK);
      end;
    end;
  end;
end;

// ------------------------------------------------------------------
// CurUninstallStepChanged
// Removes the bins directory from system PATH on uninstall.
// ------------------------------------------------------------------
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RemovePathEntry(ExpandConstant('{app}\bins'));
end;
