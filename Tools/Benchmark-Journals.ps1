# Synthetic journal performance measurement. No production journal or Playnite access.
[CmdletBinding()]
param([switch]$Large)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    & python (Join-Path $root 'Tools/Verify-Source.py')
    if($LASTEXITCODE -ne 0){throw 'Source verifier failed'}
    & python (Join-Path $root 'Tools/Verify-RC.py') --check
    if($LASTEXITCODE -ne 0){throw 'Private release guard failed'}
    & python (Join-Path $root 'Tools/Test-Package-Integrity.py')
    if($LASTEXITCODE -ne 0){throw 'Package verifier fixtures failed'}
    & dotnet restore (Join-Path $root 'SmartMetadataMatcher.csproj')
    if($LASTEXITCODE -ne 0){throw 'Extension restore failed'}
    & dotnet build (Join-Path $root 'SmartMetadataMatcher.csproj') --configuration Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'Full extension compile failed'}
    $testProject=Join-Path $root 'Tests/NameRulesTests.csproj'
    & dotnet restore $testProject
    if($LASTEXITCODE -ne 0){throw 'Test restore failed'}
    & dotnet build $testProject --configuration Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'Offline test compile failed'}
    $exe=Join-Path $root 'Tests/bin/Release/net462/SmartMetadataMatcher.Tests.exe'
    if(!(Test-Path -LiteralPath $exe)){throw 'Test runner not found'}
    & $exe
    if($LASTEXITCODE -ne 0){throw 'Offline regression tests failed'}
    Write-Host '=== SYNTHETIC JOURNAL REPLAY BENCHMARK ==='
    if($Large){& $exe --benchmark-journals-large}
    else {& $exe --benchmark-journals}
    if($LASTEXITCODE -ne 0){throw 'Synthetic benchmark validation failed'}
    Write-Host 'NOTE: Timings are diagnostic, not pass/fail criteria or proof of runtime behavior.'
    Write-Host 'NOTE: User journals, Playnite metadata, and ROM files were not accessed.'
}
finally{Pop-Location}
