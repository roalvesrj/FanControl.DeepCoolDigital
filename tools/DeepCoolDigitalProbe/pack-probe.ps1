# Builds the release zip for the standalone probe.
# The zip contains our executable, Microsoft's BCL assemblies (MIT) and setup-probe.ps1; FanControl's
# and third-party assemblies are copied from the user's installation by the bootstrap script.
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'DeepCoolDigitalProbe.zip')
)

$ErrorActionPreference = 'Stop'
$bin = Join-Path $PSScriptRoot 'bin\Release'

$files = @(
    'DeepCoolDigitalProbe.exe',
    'DeepCoolDigitalProbe.exe.config',
    'FanControl.DeepCoolDigital.Core.dll',
    'System.Buffers.dll',
    'System.Memory.dll',
    'System.Numerics.Vectors.dll',
    'System.Runtime.CompilerServices.Unsafe.dll',
    (Join-Path $PSScriptRoot 'setup-probe.ps1')
)

$resolvedFiles = @()

foreach ($file in $files) {
    $resolved = if ([System.IO.Path]::IsPathRooted($file)) { $file } else { Join-Path $bin $file }

    if (-not (Test-Path -LiteralPath $resolved)) {
        Write-Error "Missing file: $resolved. Build the solution first."
        exit 1
    }

    $resolvedFiles += $resolved
}

$staging = Join-Path $env:TEMP ('deepcool-probe-pack-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging | Out-Null

try {
    foreach ($resolved in $resolvedFiles) {
        Copy-Item -LiteralPath $resolved -Destination $staging
    }

    if (Test-Path -LiteralPath $OutputPath) {
        Remove-Item -LiteralPath $OutputPath -Force
    }

    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $OutputPath
    Write-Host "Probe package written to: $OutputPath"
}
finally {
    Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
}
