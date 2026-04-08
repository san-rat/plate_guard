#define MyAppName "PlateGuard"
#define MyAppPublisher "PlateGuard"
#define MyAppExeName "PlateGuard.exe"
#define MyAppVersion GetStringFileInfo(AddBackslash(SourcePath) + "..\builds\windows-release\publish\" + MyAppExeName, "ProductVersion")
#define MyAppSourceDir AddBackslash(SourcePath) + "..\builds\windows-release\publish"

[Setup]
AppId={{B2D87E72-6B0C-4B95-9F40-9DE90D5A7E61}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\builds\windows-release\installer
OutputBaseFilename=PlateGuard-Setup
SetupIconFile=..\src\PlateGuard.App\Assets\favicon.ico
WizardStyle=modern
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{localappdata}\PlateGuard"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
