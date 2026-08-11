#requires -Version 5.1
# FormFiller release publish script.
#
# Publishes the WPF app as a self-contained win-x64 build, then signs the
# published outputs:
#   - No -CertificateThumbprint: reuse scripts\sign-sac.ps1 (self-signed
#     "FormFiller Development" cert for Smart App Control; no-op without it).
#   - With -CertificateThumbprint: sign with that certificate instead, e.g. the
#     commercial code-signing cert for customer distribution.
#
# Idempotent: the publish folder is recreated on every run so no stale files
# survive a re-publish.

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$PublishDir = (Join-Path $PSScriptRoot '..\artifacts\publish'),
    [string]$CertificateThumbprint = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $repoRoot 'src\FormFiller.App\FormFiller.App.csproj'
$signScript = Join-Path $PSScriptRoot 'sign-sac.ps1'

if (-not (Test-Path -LiteralPath $csproj)) { throw "Project not found: $csproj" }
if (-not (Test-Path -LiteralPath $signScript)) { throw "sign script not found: $signScript" }

# Recreate the publish folder so a re-run never leaves stale files behind.
if (Test-Path -LiteralPath $PublishDir) { Remove-Item -LiteralPath $PublishDir -Recurse -Force }
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

# Self-contained: the target PC running StarSoft is unknown and may not have the
# .NET 8 runtime installed. Bundling the runtime removes that install-time
# dependency (the price is a larger installer, paid once).
# PublishTrimmed stays OFF: WPF and FlaUI/UIA3 rely on runtime reflection that
# trimming breaks; trimming a WPF app is not a supported path.
Write-Host "Publishing FormFiller ($Configuration / $Runtime / self-contained) -> $PublishDir"
dotnet publish $csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishTrimmed=false `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

$exe = Join-Path $PublishDir 'FormFiller.App.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Publish output missing: $exe" }

if ($CertificateThumbprint) {
    # Commercial certificate path (customer distribution). Sign only the app's
    # own assemblies; third-party (NuGet) binaries keep their vendor signatures.
    $cert = Get-ChildItem 'Cert:\CurrentUser\My' -CodeSigningCert |
        Where-Object { $_.Thumbprint -eq $CertificateThumbprint } |
        Select-Object -First 1
    if (-not $cert) {
        throw "Code-signing certificate with thumbprint $CertificateThumbprint not found in Cert:\CurrentUser\My."
    }
    foreach ($name in @('FormFiller.App.exe', 'FormFiller.App.dll', 'FormFiller.Core.dll')) {
        $file = Join-Path $PublishDir $name
        if (Test-Path -LiteralPath $file) {
            Set-AuthenticodeSignature -FilePath $file -Certificate $cert -HashAlgorithm SHA256 | Out-Null
        }
    }
    Write-Host "Signed FormFiller outputs with certificate $CertificateThumbprint."
}
else {
    # Dev path: mirror the post-Build hook already wired into the csproj, applied
    # to the published copies. Re-signing the already-signed binaries is harmless
    # and guarantees the final package carries the signature.
    & $signScript -TargetDir $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "sign-sac.ps1 failed with exit code $LASTEXITCODE." }
}

# Report the version the installer build will read from this exe.
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Resolve-Path $exe)).FileVersion
Write-Host "Published FormFiller $version to $PublishDir"
