param([switch]$SmokeTest, [switch]$Menu, [switch]$DeathPreview)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $projectRoot 'Builds\Windows\MosquitoObservatory.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Build the Windows prototype first.' }
$preview = Join-Path $projectRoot 'Docs\Preview'
$saveDir = Join-Path $projectRoot ('TestResults\capture-save-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $preview, $saveDir | Out-Null
$name = if ($Menu) { 'menu' } elseif ($DeathPreview) { 'death-feedback' } else { 'gameplay' }
$arguments = @('-screen-width','1600','-screen-height','900','-screen-fullscreen','0',
    '-silent','-saveDir',('"'+$saveDir+'"'), '-capture',('"'+(Join-Path $preview "$name.png")+'"'),
    '-capture-delay','8','-quit-after-capture','-logFile',('"'+(Join-Path $projectRoot "Logs\capture-$name.log")+'"'))
if (-not $Menu) { $arguments += '-auto-play' }
if ($SmokeTest) { $arguments += '-smoke-test' }
if ($DeathPreview) { $arguments += '-death-preview' }
$startedAt = [DateTime]::UtcNow
$process = Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru
$deadline = [DateTime]::UtcNow.AddSeconds(300)
while (-not $process.WaitForExit(1000) -and [DateTime]::UtcNow -lt $deadline) { }
if (-not $process.HasExited) {
    $process.Kill()
    throw 'The capture player timed out; inspect its log.'
}
if ($process.ExitCode -ne 0) { throw "Player exited with code $($process.ExitCode)." }
if (-not (Test-Path -LiteralPath (Join-Path $preview "$name.png"))) { throw 'The player did not produce a screenshot.' }
if ((Get-Item -LiteralPath (Join-Path $preview "$name.png")).LastWriteTimeUtc -lt $startedAt) { throw 'The screenshot is stale.' }
if ($SmokeTest) {
    $report = Get-Content -Raw -LiteralPath (Join-Path $preview 'runtime-smoke.json') | ConvertFrom-Json
    if (-not $report.success) { throw 'Runtime smoke check failed; inspect Docs/Preview/runtime-smoke.json.' }
}
Write-Host "Captured $name.png successfully."
