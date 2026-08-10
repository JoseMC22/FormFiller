param(
    [Parameter(Mandatory = $true)]
    [string]$TargetDir
)

# Smart App Control on this machine blocks freshly compiled unsigned binaries.
# It does NOT block Microsoft/NuGet-published files (they carry trusted
# signatures or established reputation), so only this project's own outputs
# are signed. testhost / TestPlatform / xunit files are deliberately left
# untouched: re-signing them with an unknown certificate is what made the
# test host get blocked. No-op when the certificate is absent.

$ErrorActionPreference = 'SilentlyContinue'

$cert = Get-ChildItem 'Cert:\CurrentUser\My' -CodeSigningCert |
    Where-Object { $_.Subject -match 'CN=FormFiller Development' } |
    Select-Object -First 1

if (-not $cert) {
    Write-Host 'SAC signing certificate not found; skipping sign step.'
    exit 0
}

if (-not (Test-Path -LiteralPath $TargetDir)) {
    Write-Host "TargetDir not found: $TargetDir"
    exit 0
}

Get-ChildItem -LiteralPath $TargetDir -Recurse -Include *.dll, *.exe -File |
    Where-Object { $_.Name -in @('FormFiller.Core.dll', 'FormFiller.App.dll', 'FormFiller.App.exe') } |
    ForEach-Object {
        Set-AuthenticodeSignature -FilePath $_.FullName -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    }

Write-Host 'Signed FormFiller outputs for Smart App Control.'
exit 0
