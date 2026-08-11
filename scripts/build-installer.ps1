#requires -Version 5.1
# FormFiller installer build script.
#
# Compiles installer\FormFiller.iss with Inno Setup against the published
# output in artifacts\publish. The version is read from the published exe so
# the installer always matches the binaries it packages.
#
# Prerequisites: scripts\publish-release.ps1 must have run first.
# Idempotent: the previous installer with the same version is removed before
# recompiling; ISCC overwrites its own output anyway.

[CmdletBinding()]
param(
    [string]$PublishDir = (Join-Path $PSScriptRoot '..\artifacts\publish'),
    [string]$IssFile = (Join-Path $PSScriptRoot '..\installer\FormFiller.iss'),
    [string]$ISCCPath = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$exe = Join-Path $PublishDir 'FormFiller.App.exe'
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Published app not found at $exe. Run scripts\publish-release.ps1 first."
}

# Read the file version off the published exe (e.g. 1.0.0.0) and trim the
# trailing ".0" revision so the setup name stays clean: 1.0.0.0 -> 1.0.0.
$info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
$parts = $info.FileVersion.Split('.')
while ($parts.Length -gt 3 -and $parts[-1] -eq '0') {
    $parts = $parts[0..($parts.Length - 2)]
}
$version = $parts -join '.'
if ($version -notmatch '^\d+(\.\d+){1,3}$') {
    throw "Unexpected version string from published exe: '$($info.FileVersion)'."
}

if (-not $ISCCPath) {
    $candidate = Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'
    if (Test-Path -LiteralPath $candidate) { $ISCCPath = $candidate }
}
if (-not $ISCCPath) { throw "ISCC.exe not found. Install Inno Setup 6 or pass -ISCCPath." }
if (-not (Test-Path -LiteralPath $ISCCPath)) { throw "ISCC.exe not found at: $ISCCPath" }

$outputDir = Join-Path $PSScriptRoot '..\artifacts'
$installer = Join-Path $outputDir "FormFillerSetup-$version.exe"
if (Test-Path -LiteralPath $installer) { Remove-Item -LiteralPath $installer -Force }

Write-Host "Building installer FormFillerSetup-$version.exe (Inno Setup: $ISCCPath)"
# AppSourceDir is quoted for ISCC so paths with spaces survive the /D define.
& $ISCCPath "/DAppVersion=$version" "/DAppSourceDir=`"$PublishDir`"" $IssFile
if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE." }

if (-not (Test-Path -LiteralPath $installer)) {
    throw "Expected installer not produced: $installer"
}

$sizeMB = [math]::Round((Get-Item -LiteralPath $installer).Length / 1MB, 2)
$fileCount = (Get-ChildItem -LiteralPath $PublishDir -Recurse -File).Count

Write-Host ''
Write-Host 'Build summary:'
Write-Host "  Installer: $installer"
Write-Host "  Size:      $sizeMB MB"
Write-Host "  Version:   $version"
Write-Host "  Files in package: $fileCount"
