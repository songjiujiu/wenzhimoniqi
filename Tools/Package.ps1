param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$buildRoot = Join-Path $projectRoot 'Builds\Windows'
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'Builds\Releases' }
$required = @('MosquitoObservatory.exe', 'UnityPlayer.dll', 'MonoBleedingEdge', 'MosquitoObservatory_Data')
foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $buildRoot $name))) { throw "Missing build dependency: $name. Run Tools/Unity.ps1 -Action Build first." }
}
$packageName = 'MosquitoObservatory-Windows-x64-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
$staging = Join-Path $projectRoot ('Builds\PackageStaging\' + [Guid]::NewGuid().ToString('N'))
$gameRoot = Join-Path $staging 'MosquitoObservatory'
New-Item -ItemType Directory -Force -Path $gameRoot, $OutputDirectory | Out-Null
Get-ChildItem -LiteralPath $buildRoot -Force | Where-Object {
    $_.Name -notlike '*_DoNotShip' -and $_.Extension -ne '.pdb'
} | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $gameRoot -Recurse }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Installer\ThirdPartyNotices.txt') -Destination $gameRoot -Force
$instructions = @'
蚊群观察室 · Windows 64 位试玩版

1. 右键 ZIP 压缩包，选择“全部解压缩”。请勿在压缩包预览窗口直接运行。
2. 打开解压后的 MosquitoObservatory 文件夹，双击 MosquitoObservatory.exe。
3. exe、UnityPlayer.dll、MosquitoObservatory_Data、MonoBleedingEdge 必须保持在同一个文件夹中。
   发给别人时请发送整个 ZIP，不能只发送 exe；移动游戏时也请移动整个文件夹。

无需安装 Unity 或 Blender。
按 1 / 2 / 3 切换武器；点击场景或空格使用；Esc 暂停。

如果提示缺少 UnityPlayer.dll，请重新完整解压，并检查上述文件是否齐全。
若安全软件提示隔离文件，请先核对隔离记录和文件来源；不要从陌生网站单独下载 DLL。
这是开发中的可玩原型，正式美术与性能验收仍在推进。
'@
[IO.File]::WriteAllText((Join-Path $gameRoot '开始游戏.txt'), $instructions, [Text.UTF8Encoding]::new($true))
$archive = Join-Path $OutputDirectory ($packageName + '.zip')
Compress-Archive -LiteralPath $gameRoot -DestinationPath $archive -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
[IO.File]::WriteAllText(($archive + '.sha256'), "$hash  $([IO.Path]::GetFileName($archive))`r`n", [Text.Encoding]::ASCII)
Write-Host "Packaged: $archive"
Write-Host "SHA256: $hash"
