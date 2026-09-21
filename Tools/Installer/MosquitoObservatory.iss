#ifndef BuildRoot
  #define BuildRoot "..\..\Builds\Windows"
#endif
#ifndef ReleaseRoot
  #define ReleaseRoot "..\..\Builds\Releases"
#endif
#define GameVersion "0.1.4"

[Setup]
AppId={{218B8466-634B-4B60-9A4B-A33CDF929775}
AppName=蚊群观察室
AppVersion={#GameVersion}
AppPublisher=蚊群观察室项目组
AppPublisherURL=https://github.com/songjiujiu/wenzhimoniqi
DefaultDirName={localappdata}\Programs\MosquitoObservatory
DefaultGroupName=蚊群观察室
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0
OutputDir={#ReleaseRoot}
OutputBaseFilename=MosquitoObservatory-Setup-{#GameVersion}-Windows-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\MosquitoObservatory.exe
UninstallDisplayName=蚊群观察室 {#GameVersion}
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
VersionInfoVersion={#GameVersion}.0
VersionInfoDescription=蚊群观察室 Windows 安装程序

[Languages]
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
chinesesimp.DesktopShortcut=创建桌面快捷方式
chinesesimp.LaunchGame=启动蚊群观察室
english.DesktopShortcut=Create a desktop shortcut
english.LaunchGame=Launch Mosquito Observatory

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"

[Files]
Source: "ThirdPartyNotices.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildRoot}\*"; DestDir: "{app}"; Excludes: "*_DoNotShip,*_DoNotShip\*,*.pdb"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\蚊群观察室"; Filename: "{app}\MosquitoObservatory.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\蚊群观察室"; Filename: "{app}\MosquitoObservatory.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\MosquitoObservatory.exe"; Description: "{cm:LaunchGame}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent
