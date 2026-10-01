# AGENTS.md

Working rules for AI agents (and human contributors) in this repository. Follow them strictly.

## 1. Language

- Everything committed or published is written in **English**: code, identifiers, comments, XML docs, commit messages, issues, pull requests, release notes, README and any public reply.
- Conversations with the repository owner may be in Portuguese, but artifacts and public communication stay in English.

## 2. Project summary

- **FanControl.DeepCoolDigital** — a [FanControl](https://github.com/Rem0o/FanControl.Releases) plugin that drives the status displays of DeepCool DIGITAL coolers (CPU temperature/usage) over USB HID, replacing DeepCool Hub for that purpose.
- Layout: `src/FanControl.DeepCoolDigital` (net48 plugin) + `src/FanControl.DeepCoolDigital.Core` (netstandard2.0, pure and unit-tested) + `tests/` (NUnit, net8.0) + `tools/DeepCoolDigitalProbe` (standalone CLI).
- Sensor source: FanControl's own sensors over `FanControl.IPC` (named pipe) by default; local LibreHardwareMonitor instance + Windows kernel counters as fallback.
- Device families: AG (hardware-tested), AK (implemented, awaiting community testers), LS/LD/LQ/PRO/CH/LP planned.
- Local-only planning files (gitignored): `docs/SPEC.md` (phased plan), `docs/references/` (protocol references, GPL-3.0).
- License: GPL-3.0-or-later (the protocol references it builds on are GPL-3.0).

## 3. Response patterns

### Bugs

1. Reproduce before claiming anything: read the exact code path, run the probe, or capture the log.
2. Gather the necessary information first: cooler model and USB ids (`DeepCoolDigitalProbe list`), plugin log with `logLevel=verbose`, FanControl version, Windows version, exact steps.
3. Never propose a fix for a cause that was not confirmed. If the cause is unknown, say so and list what evidence is missing.

### Features

1. Check `docs/SPEC.md` first; work belongs to a wave/item. Keep scope to what the owner approved.
2. Prefer the smallest change that fits the existing architecture (registry + capabilities + protocol classes; policy logic in Core with tests).
3. Mark unvalidated device support as experimental (🧪) in the README until a tester confirms on real hardware.

### Questions and support

1. Look at the repository files and search official sources (FanControl docs/wiki, `getfancontrol.com/docs`, upstream repos) before answering.
2. **Never state behavior that was not actually validated** — by reading the exact code, by a test, by a probe run, or by live evidence. When something is unverified, say so explicitly.
3. "It compiles" is not validation of behavior. "The API exists" is not validation that it works (see lessons).

### Public communication

- **Never post a public comment** (issue, PR, release note, discussion) without first presenting the draft to the repository owner and receiving explicit approval.
- Upstream replies are in English and must be factual; acknowledge mistakes plainly.

## 4. Mandatory workflow

Every task follows these six stages. No stage may be skipped; a stage may be lightweight when the context is trivial, but it must be consciously acknowledged.

### 4.1 Understand

- Restate the goal, the constraints and the acceptance criteria in your own words.
- Check `docs/SPEC.md` (which wave/item) and this file; confirm scope with the owner whenever anything is ambiguous.
- Define upfront what evidence will prove success.

### 4.2 Investigate

- Read the exact code path involved; never rely on names, assumptions or neighboring code.
- Reproduce the behavior first: probe run, log capture, test, or live evidence.
- Identify the root cause. If it is still unknown, say so explicitly and list the missing evidence instead of guessing.

### 4.3 Implement

- Make the smallest change that fits the architecture (registry + capabilities + protocol classes; policy logic in Core, unit-tested).
- Follow the skill standards that apply (section 5). All artifacts in English.
- Keep public behavior backward compatible unless the owner approved a change.

### 4.4 Validate

- `dotnet build -c Release` and the full NUnit suite must be green.
- Exercise the **exact production path end-to-end** (probe, deployed plugin, real log) — a neighboring API or "it compiles" is not validation.
- Hardware-facing changes are validated by the owner on real hardware before any merge.

### 4.5 Auto code review (mandatory)

- Before **handing any change to the owner for validation**, and before any merge, release, or public claim about a change, dispatch an **independent reviewer subagent** using the `requesting-code-review` skill over the git range. Un-reviewed work is never pushed for validation (the v0.3.1 incident: a fix was handed over un-reviewed and contained a real bug).
- Add the pertinent skill perspectives (section 5) — `csharp-docs`, `csharp-nunit`, `dotnet-best-practices`, `security-and-hardening` — and require each one to argue from its own lens.
- **The perspectives must "discuss":** confront the reviewers' findings against each other, against the code, and against the evidence; reconcile conflicts explicitly (a finding valid under one lens may be invalid under another).
- Reach a **common denominator** and present it to the owner — **the owner always decides** what is accepted, deferred or rejected.
- Fix every Critical/Important finding before proceeding; track Minor findings for later.
- Record the review outcome (strengths, issues, decisions) in the task summary.

### 4.6 Deliver

- Follow the publication pattern (section 9).
- Update `docs/SPEC.md` status (local) and keep README/AGENTS consistent with what actually shipped.

## 5. Skills: which one, when

The context is the soul: choose skills by the situation, not by a rigid table. This mapping is guidance, not stone — combine and skip as the task demands.

- Any C# code change → `dotnet-best-practices`
- Public/internal API surface, XML docs → `csharp-docs`
- Tests → `csharp-nunit`
- Before merge/release/major claim → `requesting-code-review` (independent reviewer, workflow 4.5)
- Code touching external input, IPC, file parsing, dependencies or trust boundaries → `security-and-hardening` (desktop-scope adaptation)
- Unsure which skill fits, or a capability is missing → `find-skills`
- Editing opencode's own configuration → `customize-opencode`
- Web-security skills (`security-best-practices`, `security-requirement-extraction`) and `solidity-security` → not applicable to this repository today; revisit only if the context changes
- `frontend-design` → not applicable (this repository has no UI)

Examples of context-driven combinations: a new config parser = `dotnet-best-practices` + `security-and-hardening` + `csharp-nunit`; a protocol addition = `dotnet-best-practices` + `csharp-docs` + `csharp-nunit` + `requesting-code-review`. When skills disagree, workflow 4.5 reconciliation applies.

## 6. Commands

```powershell
# Build everything
dotnet build FanControl.DeepCoolDigital.sln -c Release

# Run the test suite (NUnit)
dotnet test tests\FanControl.DeepCoolDigital.Tests\FanControl.DeepCoolDigital.Tests.csproj -c Release

# Probe CLI (run elevated while FanControl runs as administrator)
tools\DeepCoolDigitalProbe\bin\Release\DeepCoolDigitalProbe.exe list
tools\DeepCoolDigitalProbe\bin\Release\DeepCoolDigitalProbe.exe sensors --filter CPU
tools\DeepCoolDigitalProbe\bin\Release\DeepCoolDigitalProbe.exe temp 42 --seconds 10
tools\DeepCoolDigitalProbe\bin\Release\DeepCoolDigitalProbe.exe usage 37 --seconds 10
```

- Deploy locally: close FanControl, copy `FanControl.DeepCoolDigital.dll`, `FanControl.DeepCoolDigital.Core.dll` and `DeepCoolDigital.ini` into `<FanControl>\Plugins\`, restart FanControl.
- CI (`.github/workflows/build.yml`): build + tests + probe smoke on every push (main/develop) and pull request.

## 7. Restrictions

- **Never push to `main` directly.** All work happens on `develop`; `main` only receives `--no-ff` merge commits after owner validation and code review.
- **Never push, tag, or create releases unless the owner explicitly asks.**
- Before every commit, check the branch (`git branch --show-current`) and the status (`git status -sb`); never commit to `main` by accident (this happened once — see lessons).
- No public comments/issues/replies without prior owner approval (section 3).
- Do not add runtime dependencies beyond what FanControl already ships (HidSharp, LibreHardwareMonitor, FanControl.IPC, Grpc.*, Google.Protobuf, BCL). Compile-time references use `Private=false` so the host assemblies are used at runtime.
- `docs/` is intentionally gitignored; never commit it.
- Never commit secrets, tokens or machine-specific paths.

## 8. Accumulated lessons

- **`ReadSensorValues` (v0.3.0 incident).** The FanControl.IPC proto declares `ReadSensorValues`, and FanControl V281 answers it with `Unimplemented`. Probes had validated `GetAllSensors` only, while the plugin's production read path used `ReadSensorValues` and silently fell back to the local source — the log showed a 30-second reconnect loop that nobody noticed, and the upstream maintainer caught the mistake publicly. **Lesson: validate the exact production code path end-to-end, not just a neighboring API; read the logs of the real deployed artifact.**
- **Code review is a mandatory stage, not an optional courtesy.** The v0.3.1 fix skipped it once; workflow 4.5 exists because of that. No merge or release without it.
- **Probe dependency pitfall.** Standalone tools must reference FanControl's exact dependency set (e.g. `System.Memory` 4.0.5.0 from the release archive with auto binding redirects); NuGet versions mismatch the proto-generated bindings and fail with `TypeInitializationException`/`FileLoadException`.
- **`Environment.TickCount` wraps** (~49.7 days); retry/backoff arithmetic must be wrap-safe (32-bit unchecked) and tested.
- **Plugin sensors are namespaced by FanControl** (e.g. `DeepCool DIGITAL Display/DeepCoolDigital/CpuTemperature`); keep sensor ids stable so user curves survive upgrades.

## 9. Publication pattern

1. Work on `develop`; CI green (build + full test suite + probe smoke).
2. Local validation by the owner: deploy the develop build (3 files, FanControl closed), test on real hardware, check `DeepCoolDigital.log`.
3. Workflow 4.5 auto code review completed, with Critical/Important findings fixed and the common denominator presented to the owner.
4. Merge `develop` → `main` with `--no-ff`; push `main`.
5. Tag `vX.Y.Z`; create the GitHub release with the plugin zip + probe zip and English release notes.
6. Public communication (community submission, upstream replies): draft → owner approval → post.
7. Update `docs/SPEC.md` status (local).
