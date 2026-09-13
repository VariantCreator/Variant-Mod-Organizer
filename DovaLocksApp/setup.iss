#ifndef PublishDir
#define PublishDir "Ready-Organizer"
#endif
[Setup]
AppId={{47B4B066-0BB3-46D9-BC30-EA86F117E8AD}
AppName=Variant Mod Organizer
AppVersion=1.6.0
AppPublisher=Dova
AppPublisherURL=https://variantinteractivemap.org
AppSupportURL=https://github.com/VariantCreator/Variant-Mod-Organizer/issues
DefaultDirName={localappdata}\Programs\Variant Mod Organizer
DefaultGroupName=Variant Mod Organizer
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\app-release
OutputBaseFilename=Variant-Mod-Organizer-Installer
SetupIconFile=Resources\Dova.ico
UninstallDisplayIcon={app}\Variant-Mod-Organizer.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{autoprograms}\Variant Mod Organizer"; Filename: "{app}\Variant-Mod-Organizer.exe"
Name: "{autodesktop}\Variant Mod Organizer"; Filename: "{app}\Variant-Mod-Organizer.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\Variant-Mod-Organizer.exe"; Description: "Open Variant Mod Organizer"; Flags: nowait postinstall skipifsilent

Filename: "{app}\Variant-Mod-Organizer.exe"; Flags: nowait; Check: IsOrganizerUpdate

[Code]
function IsOrganizerUpdate: Boolean;
begin
  Result := ExpandConstant('{param:ORGANIZERUPDATE|0}') = '1';
end;

[InstallDelete]
Type: files; Name: "{app}\Dova-Locks.exe"
Type: files; Name: "{autoprograms}\Dova Locks.lnk"
Type: files; Name: "{autodesktop}\Dova Locks.lnk"

[Registry]
Root: HKCU; Subkey: "Software\Classes\VariantOrganizer.LockSave"; ValueType: string; ValueData: "Dova Locks save"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\VariantOrganizer.LockSave\shell\open\command"; ValueType: string; ValueData: """{app}\Variant-Mod-Organizer.exe"" --open-lock-save ""%1"""
Root: HKCU; Subkey: "Software\Classes\.sav\OpenWithProgids"; ValueType: none; ValueName: "VariantOrganizer.LockSave"; Flags: uninsdeletevalue
