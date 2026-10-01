# FanControl.DeepCoolDigital

[![build](https://github.com/roalvesrj/FanControl.DeepCoolDigital/actions/workflows/build.yml/badge.svg)](https://github.com/roalvesrj/FanControl.DeepCoolDigital/actions/workflows/build.yml)
[![license: GPL-3.0-or-later](https://img.shields.io/badge/license-GPL--3.0--or--later-blue.svg)](LICENSE)

A [FanControl](https://github.com/Rem0o/FanControl.Releases) plugin that drives the status display of DeepCool **DIGITAL** air coolers with live CPU data, so you no longer need DeepCool Hub running in the background just to show numbers on the cooler.

The display can be configured to show:

- **CPU temperature only**
- **CPU usage only**
- **Dynamic** — alternates between temperature and usage every few seconds, the same behaviour as DeepCool Hub's "Dynamic mode"

## Table of Contents

- [Features](#features)
- [Supported devices](#supported-devices)
- [Requirements](#requirements)
- [Installation](#installation)
- [Configuration](#configuration)
- [Replacing DeepCool Hub](#replacing-deepcool-hub)
- [How it works](#how-it-works)
- [Troubleshooting](#troubleshooting)
- [Probe tool](#probe-tool)
- [Building from source](#building-from-source)
- [Project structure](#project-structure)
- [Credits](#credits)
- [Contributing](#contributing)
- [License](#license)

## Features

- Reads CPU temperature and usage **from FanControl's own sensors over its IPC channel** when available — while the channel is healthy there is no second LibreHardwareMonitor instance, no extra driver and no extra service. Falls back automatically to a local LibreHardwareMonitor instance and Windows kernel counters when the channel is unavailable, and releases that fallback again once the IPC has been healthy for ~30 seconds.
- Registers a **`DeepCool Display CPU Temp`** sensor inside FanControl, usable in any fan curve.
- **Multiple displays at once**: every supported display identity found on the system is driven independently (one session per USB identity — two identical coolers share a session), with optional per-device settings.
- High-temperature alert on the cooler, matching DeepCool Hub's behavior (at or above 90 °C by default), toggleable per device.
- Values beyond what a display can render are clamped to all-nines (e.g. `99` on the AG), exactly like DeepCool Hub does.
- Fahrenheit support on device families that accept it (e.g. the AK series).
- **Hot-reload configuration**: edit the ini file and the change is applied in about a second — no FanControl restart.
- Automatic reconnect: keeps retrying every 5 seconds when the display is missing or busy (re-plug, sleep/resume, USB reset, DeepCool Hub started by accident).
- A tiny CLI probe (`DeepCoolDigitalProbe`) that talks to the display without FanControl — useful for testing and for adding support to new devices.
- No effect on fan control: the plugin never touches fan curves or controls.

## Supported devices

| Device | Status |
| ------ | ------ |
| DeepCool AG620 DIGITAL | ✅ tested — VID `0x3633`, PID `0x0008`, HID name `AG-DIGITAL` |
| DeepCool AG300 / AG400 / AG500 DIGITAL | ⚠️ same protocol as the tested AG620 (PID `0x0008`), untested — feedback welcome |
| DeepCool AK400 DIGITAL (PID `0x0001`) | 🧪 protocol implemented, tests green — awaiting a tester with the hardware |
| DeepCool AK620 DIGITAL (PID `0x0002`) | 🧪 protocol implemented, tests green — awaiting a tester with the hardware |
| DeepCool AK500 DIGITAL (PID `0x0003`) | 🧪 protocol implemented, tests green — awaiting a tester with the hardware |
| DeepCool AK500S DIGITAL (PID `0x0004`) | 🧪 protocol implemented, tests green — awaiting a tester with the hardware |
| Other DeepCool DIGITAL models (LS, LD, LQ, PRO, NYX, CH, LP, AIO LCD…) | ❌ different packet formats; support is planned in stages |

Legend: ✅ validated on real hardware by the maintainers • 🧪 code-complete with unit tests, awaiting hardware validation • ⚠️ expected to work, untested.

Run `DeepCoolDigitalProbe list` to see what your machine reports; device reports (model + PID + log) are welcome in the issue tracker.

## Requirements

- Windows 10 / 11
- **FanControl V238 or newer** (tested with V281). The plugin targets .NET Framework 4.8, so it works with both the `.NET 10` and the `net 4.8` FanControl distributions.
- A DeepCool DIGITAL cooler connected to an internal USB 2.0 header

> **DeepCool Hub must not run at the same time.** The display is a USB HID device and the official software keeps writing to it. Close `DeepCool.exe` from the tray and stop its services — see [Replacing DeepCool Hub](#replacing-deepcool-hub).

> **Do I need DeepCool Hub installed at all?** No. Windows recognizes the display as a standard USB HID device and the plugin talks to it directly; FanControl itself provides everything else the plugin needs (plugin API, HidSharp, LibreHardwareMonitor and its sensor driver). The only vendor-only task that still requires the Hub is updating the cooler's firmware — run it once for that, close it, and keep it disabled while FanControl manages the display.

## Installation

1. Close FanControl.
2. Copy **all three** files below into the `Plugins` folder of your FanControl installation (e.g. `C:\Program Files (x86)\FanControl\Plugins\`):
   - `FanControl.DeepCoolDigital.dll`
   - `FanControl.DeepCoolDigital.Core.dll`
   - `DeepCoolDigital.ini`
3. Start FanControl.

> **Note:** the v0.1.0 release assets predate the testable-core split and contain only two files. Releases from v0.2.0 on ship all three — or [build from source](#building-from-source).

The `DeepCool Display CPU Temp` sensor appears in the temperature list and the cooler display starts showing the configured mode. No installer and no other files: `HidSharp` and `LibreHardwareMonitor` are loaded from FanControl's own folder.

## Configuration

All settings live in `DeepCoolDigital.ini`, next to the plugin dll (`Plugins\DeepCoolDigital.ini`).

| Key | Default | Description |
| --- | ------- | ----------- |
| `mode` | `temp` | `temp` = CPU temperature only • `usage` = CPU usage only • `dynamic` = alternate between both every `autoSwitchSeconds` (also accepts `temperature`, `load`, `both`, `auto`) |
| `autoSwitchSeconds` | `5` | Seconds between temperature and usage when `mode=dynamic` |
| `alarmTemperature` | `90` | The cooler's high-temperature alert is triggered when the temperature **reaches or exceeds** this value (°C) |
| `alarmEnabled` | `true` | Enables the high-temperature alert (DeepCool Hub "Warning Control") |
| `fahrenheit` | `false` | Shows temperatures in °F on devices that support it (e.g. the AK series); ignored elsewhere |
| `sensorSource` | `auto` | Where readings come from: `auto` (FanControl IPC with local fallback), `fancontrol` (IPC only) or `local` (LibreHardwareMonitor + kernel only) |
| `usageSensor` | `CPU Total` | Name or identifier of the FanControl usage sensor used for the display (e.g. `CPU Total` or `/amdcpu/0/load/0`) |
| `vendorId` | `0x3633` | USB vendor id of the primary display target |
| `productId` | `0x0008` | USB product id of the primary display target |
| `logLevel` | `off` | Log verbosity: `off`, `events` (connections, config reloads, errors) or `verbose` (adds diagnostic details such as discovered sensors). The legacy `log=true` maps to `events` |
| `preferredTempSensors` | (built-in list) | `\|`-separated CPU temperature sensor names, in priority order (e.g. `CPU Package\|Core (Tctl/Tdie)`) |

The file is re-read automatically: save it and the display changes within ~1 second, no restart needed (changing `vendorId`/`productId` requires a restart).

Example:

```ini
mode=dynamic
autoSwitchSeconds=10
```

### Per-device overrides

With more than one display, or to pin different settings per cooler, add `[device:VID:PID]` sections. Keys inside a section override the global value for that USB identity only:

```ini
mode=temp
alarmEnabled=true

[device:0x3633:0x0008]
# AG620 DIGITAL
mode=dynamic

[device:0x3633:0x0002]
# AK620 DIGITAL
mode=usage
alarmTemperature=85
```

### Modes vs DeepCool Hub

DeepCool Hub exposes exactly the same three options for this device family (verified against the official application): "Display CPU temperature only", "Display CPU usage only" and "Dynamic mode — temperature and usage change every 5 seconds". The panel can only render one value at a time, so "both" always means alternating — there is no simultaneous temperature + usage mode in this hardware.

## Replacing DeepCool Hub

1. Close DeepCool Hub from the tray.
2. Stop its services (admin PowerShell):

   ```powershell
   Stop-Service "Deep Cool Display Service", "Deep Cool Helper Service"
   ```

3. If you want to keep them from coming back at boot (recommended if you no longer use the Hub):

   ```powershell
   Set-Service "Deep Cool Display Service" -StartupType Manual
   Set-Service "Deep Cool Helper Service" -StartupType Manual
   ```

4. Restart FanControl.

The display is a single HID device; if DeepCool Hub runs at the same time as this plugin, both keep overwriting each other's data. Pick one.

## How it works

The plugin implements `IPlugin2` and runs inside the FanControl process:

- **Sensors** — by default the plugin reads FanControl's own sensors through its named-pipe IPC channel (`SensorsRPC.GetAllSensors`), so the values shown on the cooler are exactly what FanControl already knows. Temperature candidates are narrowed to CPU sensors (LibreHardwareMonitor `/amdcpu` / `/intelcpu` identifiers) so a GPU hot spot can never be mistaken for the CPU, with priority `CPU Package` (Intel) → `Core (Tctl/Tdie)` / `Core (Tctl)` / `Core (Tdie)` (AMD) → highest CPU temperature; usage defaults to `CPU Total`. If the channel is unavailable, it falls back to a local CPU-only `LibreHardwareMonitor` instance (reusing the loaded PawnIO/WinRing0 driver) plus `GetSystemTimes` kernel counters, retries the IPC every 30 seconds and releases the local instance again after ~30 s of healthy IPC. `sensorSource` can pin either behavior.
- **Devices** — every supported display found at startup gets its own session (protocol + HID stream + settings) and is driven at every FanControl update cycle (≈1 Hz). Failures and reconnects are isolated per device.
- **Protocols** — each device family has its own packet builder in the core, covered by byte-level tests. The tested AG family writes a 64-byte report to VID `0x3633`, PID `0x0008`:

  | Byte | Meaning |
  | ---- | ------- |
  | 0 | Report id `0x10` |
  | 1 | `19` = temperature, `76` = usage |
  | 2 | Unused |
  | 3 | Tens digit (`value / 10 % 10`, or `9` when value > 99) |
  | 4 | Ones digit (`value % 10`, or `9` when value > 99) |
  | 5 | `1` when the temperature is at or above `alarmTemperature`, else `0` |

  The AK family uses a three-digit layout with a usage status bar (`[2]`), the alert in `[6]`, an initialization packet (`0xAA`) and an "SE" variant that omits the report id.

- The plugin also registers each display's temperature as a FanControl sensor (`DeepCool Display CPU Temp`, plus `DeepCool <model> CPU Temp` for additional devices), usable in fan curves.
- When `mode=dynamic`, the plugin alternates the report type on the configured interval; the display itself keeps the last value received.

Protocol was reverse engineered from the community projects listed in [Credits](#credits).

## Troubleshooting

**Display shows nothing / frozen, or the log says the device could not be opened**
DeepCool Hub (or its display service) is running and holds the device. Close the Hub and stop the services — see [Replacing DeepCool Hub](#replacing-deepcool-hub). The plugin reconnects within 5 seconds.

**Display disappears after sleep/resume, re-plug or USB reset**
Expected behaviour: the plugin detects the lost handle, closes it and reconnects every 5 seconds. No action needed.

**Device not found**
Run `DeepCoolDigitalProbe list` and check the product id. If your device reports a different PID, set it in the ini (`productId=0x....`) — but note that different DeepCool families use different packet formats.

**Nothing happens at all after starting FanControl**
Make sure all files from the release are directly inside the `Plugins` folder, then set `logLevel=verbose` in the ini and restart FanControl. `DeepCoolDigital.log` will tell you what the plugin is doing.

**CPU temperature not available**
Update FanControl to a recent version; the plugin relies on LibreHardwareMonitor's driver (PawnIO on V238+) being functional. If FanControl itself cannot see CPU temperatures, the plugin can't either.

**The log says the sensor source fell back to local**
FanControl's IPC channel is a named pipe served by FanControl; if it is unavailable (older build, channel changed), the plugin transparently falls back to its local sensor source and retries the IPC every 30 seconds. Set `logLevel=events` to see which source is active.

**FanControl (net 4.8 build) on Windows 7/8?**
Not supported. Windows 10/11 only.

## Probe tool

A standalone CLI that writes directly to the display, no FanControl required. Great to verify your hardware and the protocol before/without installing the plugin.

```text
DeepCoolDigitalProbe list
DeepCoolDigitalProbe sensors [--filter TEXT]
DeepCoolDigitalProbe temp  42 --seconds 10
DeepCoolDigitalProbe usage 37 --seconds 10
```

Options: `--seconds N` (duration, default 10) and `--interval MS` (write interval, default 1000). The `sensors` command lists FanControl's sensors over IPC — useful to pick `preferredTempSensors` / `usageSensor` values — and must run from an elevated prompt when FanControl runs as administrator.

Build output: `tools\DeepCoolDigitalProbe\bin\Release\DeepCoolDigitalProbe.exe`.

## Building from source

Requirements:

- Windows with .NET Framework 4.8 targeting pack
- .NET SDK (tested with 10.0) or Visual Studio 2022

```powershell
git clone https://github.com/roalvesrj/FanControl.DeepCoolDigital
cd FanControl.DeepCoolDigital
dotnet build -c Release
```

Plugin output: `src\FanControl.DeepCoolDigital\bin\Release\FanControl.DeepCoolDigital.dll` + `FanControl.DeepCoolDigital.Core.dll` + `DeepCoolDigital.ini`. Run the tests with `dotnet test`.

The `lib\` folder contains the **unmodified reference assemblies** shipped with the FanControl V281 release archive: `FanControl.Plugins.dll`, `HidSharp.dll`, `LibreHardwareMonitorLib.dll`, `FanControl.IPC.dll`, `Grpc.Core.Api.dll`, `GrpcDotNetNamedPipes.dll`, `Google.Protobuf.dll` and the `System.*` dependencies of the standalone probe (plus XML docs). They are used only for compilation; at runtime the plugin binds to FanControl's own copies, and nothing from `lib\` is redistributed in the plugin output.

## Project structure

```text
FanControl.DeepCoolDigital.sln
lib/                                      reference assemblies from the FanControl release
src/FanControl.DeepCoolDigital/           the plugin
  DeepCoolDigitalPlugin.cs                IPlugin2 entry point + update loop
  DeepCoolDisplaySession.cs               one session per display: HID + protocol + settings
  DeepCoolDisplaySensor.cs                per-device FanControl sensors
  CpuTemperatureSource.cs                 LibreHardwareMonitor wrapper
  CpuUsage.cs                             GetSystemTimes sampler
  Log.cs                                  file log with off/events/verbose levels
src/FanControl.DeepCoolDigital.Core/      pure, unit-testable core (netstandard2.0)
  Protocols/                              packet builders (AG and AK protocols)
  DeviceRegistry.cs                       VID/PID -> model + capabilities + protocol
  PluginConfig.cs                         ini parsing (global + per-device sections) + hot reload
  DeviceSettings.cs / DeviceOverride.cs   merged per-device settings
  DisplayPolicy.cs                        mode/unit/alarm decision logic
  CpuUsageCalculator.cs                   usage math
  TemperatureSensorSelector.cs            sensor selection
tests/FanControl.DeepCoolDigital.Tests/   NUnit test suite
tools/DeepCoolDigitalProbe/               standalone HID test CLI
```

## Credits

This plugin exists because of the reverse engineering work done by others:

- [Nortank12/deepcool-digital-linux](https://github.com/Nortank12/deepcool-digital-linux) (GPL-3.0) — documented and implemented the DeepCool DIGITAL HID protocol, including the AG series (`ag_series.rs`) this plugin is based on.
- [aSel1x/deepcool-digital-windows](https://github.com/aSel1x/deepcool-digital-windows) (GPL-3.0) — Windows port and protocol validation.
- [samehfido/DeepCool-AK400-digital-cooler](https://github.com/samehfido/DeepCool-AK400-digital-cooler) — early C#/HidSharp proof-of-concept for this hardware family.
- [Rem0o](https://github.com/Rem0o) — FanControl, the plugin API and the reference plugins used as documentation.
- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) — sensor backend.

## Contributing

Contributions are very welcome:

- **New device reports** — open an issue with the output of `DeepCoolDigitalProbe list` and (with `log=true`) the plugin log, stating your exact cooler model and firmware.
- **Bug reports** — issues with log snippets are the fastest path.
- **Pull requests** — keep the existing code style (no external runtime dependencies except the ones already in `lib\`), build with `dotnet build -c Release`, and describe how you tested the change.

By submitting a pull request you agree to license your contribution under the same license as this project (GPL-3.0-or-later).

## License

Copyright (C) 2026 Rômulo Alves

This project is free software: you can redistribute it and/or modify it under the terms of the **GNU General Public License, version 3 or later** — see [LICENSE](LICENSE). The GPL is used here to stay consistent with the GPL-3.0 protocol reference implementations this work is based on.
