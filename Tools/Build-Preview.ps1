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
    # Catch version/title, compile-inventory and missing-runner mistakes before
    # expensive restoration/build; this personal development workflow uses Python.
    & python (Join-Path $root 'Tools/Verify-Source.py')
    if ($LASTEXITCODE -ne 0) { throw 'Source consistency verification failed' }
    Run @('restore', $tests, '--source', 'https://api.nuget.org/v3/index.json')
    Run @('run', '--project', $tests, '--configuration', 'Release', '--no-restore')
    Run @('restore', $project, '--source', 'https://api.nuget.org/v3/index.json')
    Run @('build', $project, '--configuration', 'Release', '--no-restore')

    $dll = Join-Path $root 'bin/Release/net462/SmartMetadataMatcher.dll'
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw "Missing built DLL: $dll" }
    # Fail closed if manifest, source assembly version and compiled package disagree.
    $yaml = Get-Content -LiteralPath (Join-Path $root 'extension.yaml') -Raw
    $source = Get-Content -LiteralPath (Join-Path $root 'Source/Plugin.cs') -Raw
    $yamlVersion = [regex]::Match($yaml, '(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$').Groups[1].Value
    $sourceVersion = [regex]::Match($source, 'AssemblyVersion\("([0-9.]+)"\)').Groups[1].Value
    $dllVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString()
    if (-not $yamlVersion -or $sourceVersion -ne "$yamlVersion.0" -or $dllVersion -ne $sourceVersion) {
        throw "Version mismatch: YAML=$yamlVersion source=$sourceVersion compiled=$dllVersion"
    }
    New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
    $name = "SmartMetadataMatcher-$yamlVersion-in-app-import-drafts-preview-" + (Get-Date -Format 'yyyyMMdd-HHmmss')
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
    Write-Host 'IMPORT DRAFT PREVIEW: staged title/description edits and field exclusions, final approval, journalled undo. Five-game cap unchanged.'
}
finally { Pop-Location }
