# Build a candidate .pext from an already tested Release DLL. No GitHub actions.
[CmdletBinding()]
param([string]$OutputPath = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$manifest = Join-Path $root 'extension.yaml'
$dll = Join-Path $root 'bin/Release/net462/SmartMetadataMatcher.dll'
if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw 'Build a Release DLL first (dotnet build -c Release)' }
$yaml = Get-Content -LiteralPath $manifest -Raw
$match = [regex]::Match($yaml,'(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$')
if (-not $match.Success) { throw 'Invalid extension.yaml version' }
$version = $match.Groups[1].Value
$compiled = [System.Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString()
if ($compiled -ne "$version.0") { throw "Compiled DLL version $compiled differs from manifest $version" }
if (-not $OutputPath) { $OutputPath = Join-Path $root "Releases/SmartMetadataMatcher-$version-CANDIDATE.pext" }
$out = [System.IO.Path]::GetFullPath($OutputPath)
if (Test-Path -LiteralPath $out) { throw 'Refusing to overwrite an existing package' }
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($out)) | Out-Null
$tmp = Join-Path $env:TEMP ('SMM-Release-' + [guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path $tmp | Out-Null
    Copy-Item -LiteralPath $manifest -Destination $tmp
    Copy-Item -LiteralPath $dll -Destination $tmp
    $zip = "$out.zip"
    Compress-Archive -Path (Join-Path $tmp '*') -DestinationPath $zip -CompressionLevel Optimal
    Move-Item -LiteralPath $zip -Destination $out
    & python (Join-Path $root 'Tools/Public-Release-Check.py') --package $out
    if ($LASTEXITCODE -ne 0) { throw 'Package validation failed; do not publish' }
    Write-Host 'CANDIDATE ONLY. Windows and Playnite runtime release gates still apply.'
    Write-Host "PACKAGE: $out"
    Write-Host "SHA256: $((Get-FileHash -Algorithm SHA256 -LiteralPath $out).Hash)"
}
finally { if (Test-Path -LiteralPath $tmp) { Remove-Item -LiteralPath $tmp -Recurse -Force } }
