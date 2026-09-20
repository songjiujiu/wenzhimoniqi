param([string]$CompilerPath = $env:INNO_SETUP_COMPILER, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $CompilerPath) {
    $candidates = @(
        (Join-Path $projectRoot '.tools-cache\inno\compiler\ISCC.exe'),
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 7\ISCC.exe"
    )
    $CompilerPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath)) {
    throw 'Install Inno Setup 6.7.3 or newer from https://jrsoftware.org/isdl.php and supply -CompilerPath or INNO_SETUP_COMPILER.'
}
$buildRoot = Join-Path $projectRoot 'Builds\Windows'
foreach ($name in @('MosquitoObservatory.exe','UnityPlayer.dll','MosquitoObservatory_Data','MonoBleedingEdge')) {
    if (-not (Test-Path -LiteralPath (Join-Path $buildRoot $name))) { throw "Missing runtime dependency: $name. Build the game first." }
}
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'Builds\Releases' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$scriptPath = Join-Path $PSScriptRoot 'Installer\MosquitoObservatory.iss'
& $CompilerPath "/DBuildRoot=$buildRoot" "/DReleaseRoot=$OutputDirectory" $scriptPath
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed: $LASTEXITCODE" }
$versionMatch = [regex]::Match((Get-Content -Raw -LiteralPath $scriptPath), '#define GameVersion "([^"]+)"')
if (-not $versionMatch.Success) { throw 'Installer version is missing.' }
$installer = Join-Path $OutputDirectory ('MosquitoObservatory-Setup-' + $versionMatch.Groups[1].Value + '-Windows-x64.exe')
$hash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash
[IO.File]::WriteAllText(($installer + '.sha256'), "$hash  $([IO.Path]::GetFileName($installer))`r`n", [Text.Encoding]::ASCII)
Write-Host "Installer: $installer"
Write-Host "SHA256: $hash"
