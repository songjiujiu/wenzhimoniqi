param([Parameter(Mandatory=$true)][string]$InstallerPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$InstallerPath = (Resolve-Path -LiteralPath $InstallerPath).Path
$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{218B8466-634B-4B60-9A4B-A33CDF929775}_is1'
$desktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) '蚊群观察室.lnk'
$menuRoot = Join-Path ([Environment]::GetFolderPath('Programs')) '蚊群观察室'
$menuLink = Join-Path $menuRoot '蚊群观察室.lnk'
foreach ($existing in @($registryPath, $desktopLink, $menuRoot)) {
    if (Test-Path -LiteralPath $existing) { throw "Existing installation or shortcut found; refusing to replace it during testing: $existing" }
}
$testRoot = Join-Path $projectRoot ('TestResults\installer-' + [Guid]::NewGuid().ToString('N'))
$gameRoot = Join-Path $testRoot '安装 测试\MosquitoObservatory'
New-Item -ItemType Directory -Force -Path $testRoot | Out-Null
function Invoke-CheckedProcess([string]$File, [string[]]$Arguments, [string]$WorkingDirectory) {
    $process = Start-Process -FilePath $File -ArgumentList $Arguments -WorkingDirectory $WorkingDirectory -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(60000)) { $process.Kill(); throw "Timed out: $File" }
    if ($process.ExitCode -ne 0) { throw "Exit code $($process.ExitCode): $File" }
}
$report = [ordered]@{ installer = $InstallerPath; sha256 = (Get-FileHash -LiteralPath $InstallerPath).Hash; fileCount = 0; shortcuts = $false; runtime = $false; uninstall = $false }
try {
    Invoke-CheckedProcess $InstallerPath @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/LANG=chinesesimp',('/DIR="' + $gameRoot + '"'),('/LOG="' + (Join-Path $testRoot 'install.log') + '"')) $projectRoot
    if (-not (Test-Path -LiteralPath $registryPath)) { throw 'Uninstall registration missing.' }
    $buildRoot = Join-Path $projectRoot 'Builds\Windows'
    Get-ChildItem -LiteralPath $buildRoot -File -Recurse | Where-Object { $_.FullName -notmatch '_DoNotShip[\\/]' -and $_.Extension -ne '.pdb' } | ForEach-Object {
        $relative = $_.FullName.Substring($buildRoot.Length + 1)
        $installed = Join-Path $gameRoot $relative
        if (-not (Test-Path -LiteralPath $installed)) { throw "Missing installed file: $relative" }
        if ((Get-FileHash -LiteralPath $_.FullName).Hash -ne (Get-FileHash -LiteralPath $installed).Hash) { throw "Installed file differs: $relative" }
        $report.fileCount++
    }
    if (Get-ChildItem -LiteralPath $gameRoot -Recurse | Where-Object { $_.Name -like '*_DoNotShip' -or $_.Extension -eq '.pdb' }) { throw 'Development artifacts were included.' }
    $exe = Join-Path $gameRoot 'MosquitoObservatory.exe'
    $shell = New-Object -ComObject WScript.Shell
    foreach ($link in @($desktopLink, $menuLink)) {
        if (-not (Test-Path -LiteralPath $link)) { throw "Missing shortcut: $link" }
        $shortcut = $shell.CreateShortcut($link)
        if ($shortcut.TargetPath -ne $exe -or $shortcut.WorkingDirectory -ne $gameRoot) { throw "Incorrect shortcut target: $link" }
    }
    $report.shortcuts = $true
    Invoke-CheckedProcess $exe @('-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-silent','-auto-play','-smoke-test','-quit-after-capture',
        '-capture',('"' + (Join-Path $testRoot 'gameplay.png') + '"'),'-saveDir',('"' + (Join-Path $testRoot 'save') + '"'),'-logFile',('"' + (Join-Path $testRoot 'player.log') + '"')) $gameRoot
    $runtime = Get-Content -Raw -LiteralPath (Join-Path $testRoot 'runtime-smoke.json') | ConvertFrom-Json
    if (-not $runtime.success) { throw 'Installed game runtime smoke failed.' }
    $report.runtime = $true
} finally {
    $uninstaller = Join-Path $gameRoot 'unins000.exe'
    if (Test-Path -LiteralPath $uninstaller) {
        $resolvedTarget = (Resolve-Path -LiteralPath $gameRoot).Path
        $resolvedRoot = (Resolve-Path -LiteralPath $testRoot).Path + [IO.Path]::DirectorySeparatorChar
        if (-not $resolvedTarget.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Uninstall target escaped test directory.' }
        Invoke-CheckedProcess $uninstaller @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="' + (Join-Path $testRoot 'uninstall.log') + '"')) $testRoot
    }
}
foreach ($remaining in @($registryPath, $desktopLink, $menuLink, (Join-Path $gameRoot 'MosquitoObservatory.exe'), (Join-Path $gameRoot 'UnityPlayer.dll'))) {
    if (Test-Path -LiteralPath $remaining) { throw "Uninstall left a tracked artifact: $remaining" }
}
$report.uninstall = $true
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $testRoot 'installer-report.json') -Encoding utf8
$report | ConvertTo-Json
Write-Host "Installer validation passed. Evidence: $testRoot"
