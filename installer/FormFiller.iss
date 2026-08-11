; FormFiller - Inno Setup 6 installer script.
;
; Normally compiled via scripts\build-installer.ps1, which passes the version
; read from the published exe (/DAppVersion) and the publish folder
; (/DAppSourceDir). Manual compile:
;   ISCC.exe /DAppVersion=1.0.0 installer\FormFiller.iss

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef AppSourceDir
  #define AppSourceDir "..\artifacts\publish"
#endif

#define AppName "FormFiller"
#define AppPublisher "FormFiller"
#define AppExeName "FormFiller.App.exe"

[Setup]
AppId={{C63825C2-413B-4D99-A8EB-8B4F5E958D9F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
; Per-user install: the app only writes to %APPDATA%\FormFiller (SQLite DB +
; trial state) and needs no admin rights. Avoiding the UAC prompt also keeps
; the commercial install flow friction-free.
PrivilegesRequired=lowest
; Single Start Menu entry; the modern wizard covers the rest.
DisableProgramGroupPage=yes
OutputDir=..\artifacts
OutputBaseFilename=FormFillerSetup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes
UninstallDisplayIcon={app}\{#AppExeName}
; The publish target is win-x64 self-contained, so refuse 32-bit Windows at
; install time instead of shipping a setup that cannot run.
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
; Customer distribution: sign the setup with the commercial certificate once
; it is available (via [Setup] SignTool=, not a /D define). The dev
; self-signed cert must NOT be used to sign the setup file.

[Languages]
; English only: the product UI is English-only today, and Default.isl (the
; compiler's bundled language file) IS English. Adding Spanish would require a
; translated MessagesFile that Inno does not ship by default.
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Desktop shortcut off by default per Windows guidelines; the Start Menu entry
; is always created. Users opt in during install.
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#AppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
; "Launch FormFiller" checkbox, unchecked by default: commercial installer,
; users launch from the Start Menu. Trial enforcement is entirely in-app at
; first run, so the installer carries no licensing logic.
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent unchecked
