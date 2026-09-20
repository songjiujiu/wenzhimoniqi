param([Parameter(Mandatory=$true)][string]$PackagePath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$staging = Join-Path $projectRoot '.tools-cache\tmp-essential'
New-Item -ItemType Directory -Force -Path $staging | Out-Null
& tar -xf $PackagePath -C $staging
if ($LASTEXITCODE -ne 0) { throw 'Could not extract the installed TMP resources.' }
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Assets')) + [IO.Path]::DirectorySeparatorChar
$copied = 0
foreach ($entry in Get-ChildItem -LiteralPath $staging -Directory) {
    $pathname = Join-Path $entry.FullName 'pathname'
    if (-not (Test-Path -LiteralPath $pathname)) { continue }
    $relative = [IO.File]::ReadAllText($pathname).Trim()
    $destination = [IO.Path]::GetFullPath((Join-Path $projectRoot $relative))
    if (-not $destination.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Archive entry is outside this project's Assets folder: $relative"
    }
    $asset = Join-Path $entry.FullName 'asset'
    if (Test-Path -LiteralPath $asset) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
        Copy-Item -LiteralPath $asset -Destination $destination
    } else { New-Item -ItemType Directory -Force -Path $destination | Out-Null }
    $meta = Join-Path $entry.FullName 'asset.meta'
    if (Test-Path -LiteralPath $meta) { Copy-Item -LiteralPath $meta -Destination ($destination + '.meta') }
    $copied++
}
Write-Host "Imported $copied TMP resources from the installed Unity package."
