param(
    [ValidateSet('Setup','Test','Build','Open')][string]$Action = 'Setup',
    [string]$EditorPath = $env:UNITY_EDITOR_PATH
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$versionLine = Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt') | Select-Object -First 1
$editorVersion = ($versionLine -split ':', 2)[1].Trim()
if (-not $EditorPath) {
    $candidates = @("D:\untyle\$editorVersion\Editor\Unity.exe", "$env:ProgramFiles\Unity\Hub\Editor\$editorVersion\Editor\Unity.exe")
    $EditorPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $EditorPath -or -not (Test-Path -LiteralPath $EditorPath)) {
    throw "Unity $editorVersion was not found. Supply -EditorPath or UNITY_EDITOR_PATH."
}
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot 'Logs'), (Join-Path $projectRoot 'TestResults') | Out-Null
$arguments = @('-projectPath', ('"' + $projectRoot + '"'))
if ($Action -eq 'Open') {
    Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Normal
    exit 0
}
$arguments += @('-batchmode', '-nographics', '-logFile', ('"' + (Join-Path $projectRoot "Logs\$Action.log") + '"'))
switch ($Action) {
    'Setup' { $arguments += @('-quit', '-executeMethod', 'Mosquito.Editor.ProjectSetup.Setup') }
    'Build' { $arguments += @('-quit', '-executeMethod', 'Mosquito.Editor.ProjectSetup.BuildWindows') }
    'Test'  { $arguments += @('-runTests', '-testPlatform', 'EditMode', '-testResults', ('"' + (Join-Path $projectRoot 'TestResults\editmode.xml') + '"')) }
}
$process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity $Action failed ($($process.ExitCode)); inspect Logs\$Action.log." }
Write-Host "Unity $Action succeeded."
