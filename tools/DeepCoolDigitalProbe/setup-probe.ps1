# Copies the assemblies the standalone DeepCoolDigitalProbe needs from a FanControl installation.
# The probe zip intentionally bundles only our executable and Microsoft's BCL assemblies; FanControl's
# and third-party assemblies (HidSharp, gRPC, Protobuf) are not redistributable and are copied here.
param(
    [string]$FanControlPath
)

$ErrorActionPreference = 'Stop'
$probeDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$hostFiles = @(
    'HidSharp.dll',
    'FanControl.IPC.dll',
    'Grpc.Core.Api.dll',
    'GrpcDotNetNamedPipes.dll',
    'Google.Protobuf.dll'
)

function Test-FanControlDirectory([string]$path) {
    return $path -and (Test-Path -LiteralPath (Join-Path $path 'FanControl.exe'))
}

function Find-FanControlPath {
    if ($FanControlPath) {
        if (Test-FanControlDirectory $FanControlPath) {
            return $FanControlPath
        }

        Write-Error "The provided -FanControlPath does not contain FanControl.exe: $FanControlPath"
        exit 1
    }

    $candidates = @(
        'C:\Program Files (x86)\FanControl',
        'C:\Program Files\FanControl'
    )

    if ($env:LOCALAPPDATA) {
        $candidates += (Join-Path $env:LOCALAPPDATA 'FanControl')
    }

    if ($env:USERPROFILE) {
        $candidates += (Join-Path $env:USERPROFILE 'FanControl')
    }

    foreach ($candidate in $candidates) {
        if (Test-FanControlDirectory $candidate) {
            return $candidate
        }
    }

    $startMenus = @()

    if ($env:APPDATA) {
        $startMenus += (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs')
    }

    if ($env:ProgramData) {
        $startMenus += (Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs')
    }

    foreach ($menu in $startMenus) {
        if (-not (Test-Path -LiteralPath $menu)) {
            continue
        }

        foreach ($link in Get-ChildItem -LiteralPath $menu -Recurse -Filter '*.lnk' -ErrorAction SilentlyContinue) {
            try {
                $shell = New-Object -ComObject WScript.Shell
                $target = $shell.CreateShortcut($link.FullName).TargetPath
            }
            catch {
                continue
            }

            if ($target -and (Test-Path -LiteralPath $target) -and (Test-FanControlDirectory (Split-Path -Parent $target))) {
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
$alreadyPresent = 0
$missing = @()

foreach ($file in $hostFiles) {
    $destination = Join-Path $probeDir $file

    if (Test-Path -LiteralPath $destination) {
        $alreadyPresent++
        continue
    }

    $source = Join-Path $fanControl $file

    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination $destination -Force
        $copied++
    }
    else {
        $missing += $file
    }
}

Write-Host "FanControl folder: $fanControl"
Write-Host "Copied $copied file(s); $alreadyPresent already present."

if ($missing.Count -gt 0) {
    Write-Warning ('Not found in the FanControl folder: ' + ($missing -join ', '))
    Write-Warning 'Update FanControl or copy the missing files manually next to the probe executable.'
    exit 1
}

Write-Host 'Probe is ready. Example: .\DeepCoolDigitalProbe.exe sensors --filter CPU (run elevated)'
