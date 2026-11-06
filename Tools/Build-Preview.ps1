# Windows PowerShell 7: compile and test before creating a private preview package.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $root 'SmartMetadataMatcher.csproj'
$tests = Join-Path $root 'Tests/NameRulesTests.csproj'
$releaseDir = Join-Path (Split-Path $root -Parent) 'Releases/SmartMetadataMatcher'

function Run([string[]]$Command) {
    & dotnet @Command
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Command -join ' ') failed ($LASTEXITCODE)" }
}

Push-Location $root
try {
    Run @('restore', $tests, '--source', 'https://api.nuget.org/v3/index.json')
    Run @('run', '--project', $tests, '--configuration', 'Release', '--no-restore')
    Run @('restore', $project, '--source', 'https://api.nuget.org/v3/index.json')
    Run @('build', $project, '--configuration', 'Release', '--no-restore')

    $dll = Join-Path $root 'bin/Release/net462/SmartMetadataMatcher.dll'
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw "Missing built DLL: $dll" }
    New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
    $name = 'SmartMetadataMatcher-0.4.14-title-history-preview-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
    $zip = Join-Path $releaseDir ($name + '.zip')
    $pext = Join-Path $releaseDir ($name + '.pext')
    if (Test-Path -LiteralPath $pext) { throw "Refusing to overwrite $pext" }
    $stage = Join-Path $env:TEMP ('SMM-Package-' + [Guid]::NewGuid().ToString('N'))
    try {
        New-Item -ItemType Directory -Path $stage | Out-Null
        Copy-Item -LiteralPath $dll -Destination $stage
        Copy-Item -LiteralPath (Join-Path $root 'extension.yaml') -Destination $stage
        Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
        Move-Item -LiteralPath $zip -Destination $pext
    }
    finally { if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force } }
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $pext).Hash
    Write-Host "SUCCESS: $pext"
    Write-Host "SHA256: $hash"
    Write-Host 'TITLE/HISTORY PREVIEW: recent-history multi-select, supervised Playnite-only title rename, latest-consecutive selected undo. Backup Playnite before writes; no production bulk writes.'
}
finally { Pop-Location }
