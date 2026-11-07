# Read-only private release evidence check; compiles/tests but creates no .pext.
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project=Join-Path $root 'SmartMetadataMatcher.csproj'
$tests=Join-Path $root 'Tests/NameRulesTests.csproj'
$releaseDir=Join-Path (Split-Path $root -Parent) 'Releases/SmartMetadataMatcher'
$sha='05C79F1AD4883D70D4ADBCB0EA86852E8F00BD189F24107AE872162EE0D9A3B4'
function RunPython([string[]]$cmd) {
    & python @cmd
    if ($LASTEXITCODE -ne 0) { throw "Python validation failed: $($cmd[0])" }
}
function RunDotnet([string[]]$cmd) {
    & dotnet @cmd
    if ($LASTEXITCODE -ne 0) { throw "dotnet validation failed: $($cmd[0])" }
}
Push-Location $root
try {
    RunPython @((Join-Path $root 'Tools/Verify-Source.py'))
    RunPython @((Join-Path $root 'Tools/Test-Package-Integrity.py'))
    RunPython @((Join-Path $root 'Tools/Verify-RC.py'),'--self-test')
    RunPython @((Join-Path $root 'Tools/Verify-RC.py'),'--check')
    RunPython @((Join-Path $root 'Tools/Test-Release-Audit.py'))
    RunDotnet @('restore',$project,'--source','https://api.nuget.org/v3/index.json')
    RunDotnet @('build',$project,'--configuration','Release','--no-restore')
    RunDotnet @('restore',$tests,'--source','https://api.nuget.org/v3/index.json')
    RunDotnet @('run','--project',$tests,'--configuration','Release','--no-restore')
    $packages=@(Get-ChildItem -LiteralPath $releaseDir -Filter 'SmartMetadataMatcher-0.5.8-private-journal-replay-index-*.pext' -File |
        Where-Object { (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash -eq $sha })
    if ($packages.Count -lt 1) { throw 'Previously verified v0.5.8 indexed journal .pext was not found; no report/commit.' }
    $package=($packages | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).FullName
    RunPython @((Join-Path $root 'Tools/Verify-Package.py'),$package)
    $reportDir=Join-Path $releaseDir 'PrivateReports'
    $reportName='SMM-0.5.8-RELEASE-AUDIT-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,6)+'.json'
    $report=Join-Path $reportDir $reportName
    RunPython @((Join-Path $root 'Tools/Audit-Release-Evidence.py'),'--package',$package,
        '--expected-package-sha256',$sha,'--report',$report)
    Write-Host 'PASS: consolidated private release audit; full offline suites; existing .pext bytes verified.'
    Write-Host 'NO NEW .pext; no game/Playnite journal reads, migrations or remote actions.'
    Write-Host 'NOT VERIFIED: transitive dependency provenance, compiled binary security or genuine process-crash recovery.'
}
finally { Pop-Location }
