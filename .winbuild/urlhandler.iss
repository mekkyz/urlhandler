#define public Dependency_Path_NetCoreCheck "dependencies\"
#include "CodeDependencies.iss"

#define AppName "URL-Handler"
#define AppVersion "1.0.2"
#define Protocol "chemotion"

[Setup]
AppId={{A52940A8-3C21-4FE3-8F22-C4AFC690269A}}
AppName={#AppName}
AppPublisher=Scientifc Computing Center, KIT
AppPublisherURL=https://www.scc.kit.edu/
AppVersion={#AppVersion}
AppComments=Chemotion-URL-Handler
AppContact=SDM, SCC, KIT
AppCopyright=Copyright (C) 2024 KIT Scientific Computing Center (SCC)
DefaultDirName={commonpf64}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=C:/app/build/.
OutputBaseFilename={#AppName}-{#AppVersion}
Compression=lzma
SolidCompression=true
LicenseFile="LICENSE.rtf"
SetupIconFile="..\Assets\icon.ico"
WizardStyle=modern
UninstallDisplayIcon="..\Assets\icon.ico"
UninstallDisplayName={#AppName}

[Languages]
Name: en; MessagesFile: "compiler:Default.isl"
//Name: de; MessagesFile: "compiler:Languages\German.isl"

[Files]
Source: "..\bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\av_libglesv2.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\libHarfBuzzSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\libSkiaSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\libuv.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\URL-Handler.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name:"{group}\{#AppName}"; Filename:"{app}\{#AppName}.exe";WorkingDir:{app}

[Registry]
Root: HKCR; Subkey: "chemotion"; ValueType: string; ValueName: ""; ValueData: "Chemotion"; Flags: uninsdeletekey
Root: HKCR; Subkey: "chemotion"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Flags: uninsdeletekey
Root: HKCR; Subkey: "chemotion\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppName}.exe"" ""%1"""; Flags: uninsdeletekey

[Code]
function InitializeSetup: Boolean;
begin
  Dependency_AddDotNet80DesktopSpecificVersion('8.0.6');
  Result:=True;
end;


