# Private synthetic recovery acceptance suite. No .pext and no live Playnite data.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$extension=Join-Path $root 'SmartMetadataMatcher.csproj'
$tests=Join-Path $root 'Tests/NameRulesTests.csproj'

function RunPython([string[]]$ArgsToRun) {
    & python @ArgsToRun
    if ($LASTEXITCODE -ne 0) { throw "Python check failed: $($ArgsToRun -join ' ')" }
}
function RunDotnet([string[]]$ArgsToRun) {
    & dotnet @ArgsToRun
    if ($LASTEXITCODE -ne 0) { throw "dotnet check failed: $($ArgsToRun -join ' ')" }
}
Push-Location $root
try {
    RunPython @((Join-Path $root 'Tools/Verify-Source.py'))
    RunPython @((Join-Path $root 'Tools/Test-Package-Integrity.py'))
    RunPython @((Join-Path $root 'Tools/Verify-RC.py'),'--self-test')
    RunPython @((Join-Path $root 'Tools/Verify-RC.py'),'--check')
    RunDotnet @('restore',$extension,'--source','https://api.nuget.org/v3/index.json')
    RunDotnet @('build',$extension,'--configuration','Release','--no-restore')
    RunDotnet @('restore',$tests,'--source','https://api.nuget.org/v3/index.json')
    RunDotnet @('run','--project',$tests,'--configuration','Release','--no-restore')
    # Explicitly exercise isolated suite in its own new process as well.
    RunDotnet @('run','--project',$tests,'--configuration','Release','--no-restore','--','--recovery-fault-matrix')
    Write-Host 'PASS: full offline suites and isolated synthetic recovery fault matrix; production source unchanged.'
    Write-Host 'NO .pext GENERATED; no Playnite database, extension data or ROM changes.'
}
finally { Pop-Location }
