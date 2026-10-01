# Copies the DLLs the standalone DeepCoolDigitalProbe needs from a FanControl installation.
# The probe intentionally does not bundle FanControl's assemblies; run this script once after
# extracting the probe zip.
param(
    [string]$FanControlPath
)

$ErrorActionPreference = 'Stop'
$probeDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$requiredFiles = @(
    'HidSharp.dll',
    'FanControl.IPC.dll',
    'Grpc.Core.Api.dll',
    'GrpcDotNetNamedPipes.dll',
    'Google.Protobuf.dll',
    'System.Buffers.dll',
    'System.Memory.dll',
    'System.Numerics.Vectors.dll',
    'System.Runtime.CompilerServices.Unsafe.dll'
)

function Find-FanControlPath {
    $candidates = @(
        $FanControlPath,
        'C:\Program Files (x86)\FanControl',
        'C:\Program Files\FanControl',
        (Join-Path $env:LOCALAPPDATA 'FanControl'),
        (Join-Path $env:USERPROFILE 'FanControl')
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path (Join-Path $candidate 'FanControl.exe'))) {
            return $candidate
        }
    }

    $shell = New-Object -ComObject WScript.Shell
    $startMenus = @(
        (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'),
        (Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs')
    )

    foreach ($menu in $startMenus) {
        if (-not (Test-Path $menu)) {
            continue
        }

        foreach ($link in Get-ChildItem $menu -Recurse -Filter '*.lnk' -ErrorAction SilentlyContinue) {
            $target = $shell.CreateShortcut($link.FullName).TargetPath

            if ($target -like '*FanControl*' -and (Test-Path $target)) {
                return (Split-Path -Parent $target)
            }
        }
    }

    return $null
}

$fanControl = Find-FanControlPath

if (-not $fanControl) {
    Write-Error 'FanControl installation not found. Pass -FanControlPath "C:\path\to\FanControl".'
    exit 1
}

$copied = 0
$missing = @()

foreach ($file in $requiredFiles) {
    $source = Join-Path $fanControl $file

    if (Test-Path $source) {
        Copy-Item $source (Join-Path $probeDir $file) -Force
        $copied++
    }
    else {
        $missing += $file
    }
}

Write-Host "Copied $copied dependency file(s) from: $fanControl"

if ($missing.Count -gt 0) {
    Write-Warning ('Not found in the FanControl folder: ' + ($missing -join ', '))
    Write-Warning 'Update FanControl or copy the missing files manually next to the probe executable.'
    exit 1
}

Write-Host 'Probe is ready. Example: .\DeepCoolDigitalProbe.exe sensors --filter CPU (elevated)'
