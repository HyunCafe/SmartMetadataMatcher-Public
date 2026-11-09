# Current build entry point. Produces a candidate, never publishes or installs it.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
function Invoke-Checked {
    param([string]$Command, [string[]]$Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE" }
}
Push-Location $root
try {
    $commit = (& git rev-parse HEAD)
    if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[a-f0-9]{40}$') { throw 'Cannot identify source commit' }
    $status = (& git status --porcelain)
    if ($LASTEXITCODE -ne 0 -or $status) { throw 'Commit or stash source changes before producing a traceable candidate' }
    Invoke-Checked 'python' @('Tools/Public-Release-Check.py')
    Invoke-Checked 'python' @('Tools/Verify-Source.py')
    Invoke-Checked 'python' @('Tools/Test-Public-Package.py')
    Invoke-Checked 'python' @('Tools/Test-Package-Integrity.py')
    Invoke-Checked 'dotnet' @('restore','SmartMetadataMatcher.csproj','--source','https://api.nuget.org/v3/index.json')
    Invoke-Checked 'dotnet' @('build','SmartMetadataMatcher.csproj','-c','Release','--no-restore')
    Invoke-Checked 'dotnet' @('restore','Tests/NameRulesTests.csproj','--source','https://api.nuget.org/v3/index.json')
    Invoke-Checked 'dotnet' @('run','--project','Tests/NameRulesTests.csproj','-c','Release','--no-restore')
    $yaml = Get-Content 'extension.yaml' -Raw
    $version = [regex]::Match($yaml,'(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$').Groups[1].Value
    if (-not $version) { throw 'Missing extension version' }
    $out = Join-Path $root 'Releases/CI'
    [System.IO.Directory]::CreateDirectory($out) | Out-Null
    $filename = "SmartMetadataMatcher-$version-$($commit.Substring(0,12))-CANDIDATE.pext"
    $package = Join-Path $out $filename
    if (Test-Path -LiteralPath $package) { throw 'Candidate already exists; preserve or move the prior output before rebuilding' }
    & (Join-Path $PSScriptRoot 'Package-Release.ps1') -OutputPath $package
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $package).Hash.ToLowerInvariant()
    "$hash  $filename" | Set-Content (Join-Path $out 'SHA256SUMS.txt') -Encoding ascii
    $evidence = [ordered]@{
        source_commit = $commit
        extension_version = $version
        package = $filename
        package_sha256 = $hash
        dll_sha256 = (Get-FileHash 'bin/Release/net462/SmartMetadataMatcher.dll' -Algorithm SHA256).Hash.ToLowerInvariant()
        automated_checks = @('Windows Release build','offline test executable','source checks','package verifier fixtures','packaged DLL and manifest byte comparison')
        release_status = 'Candidate only; no Playnite runtime acceptance or security certification implied'
    }
    $evidence | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $out 'build-evidence.json') -Encoding utf8
    Write-Host "PASS: candidate built from $commit"
    Write-Host "CANDIDATE: $package"
}
finally { Pop-Location }
