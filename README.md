# KR Desktop Hub CoreHost

> CoreHost `v2.0.0` is the delivered portable Windows 11 CoreHost platform. Production business Widgets live in the independent `kairosrepublica/kr-desktop-hub-widgets` repository and must not be merged back into CoreHost.

Portable-first, extensible desktop CoreHost for Windows 11 Widgets.

## Download

Use the latest GitHub Release:

```text
Releases -> v2.0.0 -> KRDesktopHub_CoreHost_win-x64_portable_v2.0.0.zip
```

Portable usage:

```text
extract
run START_KR_DESKTOP_HUB.cmd
```

## Current status

```text
Project stage:
CoreHost v2.0.0 delivered

Target platform:
Windows 11 x64

CoreHost popup:
Owner-controlled viewport
600-DIP default width
600-DIP minimum width
240-DIP minimum height
host-level overflow scrolling
hide-to-tray lifecycle

Widget-host state:
persistent
transactional
discovery-free for ordinary state-only transitions

Installed-catalog refresh:
pure staged discovery
acceptance before mutation
exact accepted-catalog reconciliation

Windows shell:
ShowActivated = false
ShowInTaskbar = false
ordinary Show does not force Activate()
WS_EX_NOACTIVATE applies to the main tray popup only
title-bar minimize hides to tray
title-bar close hides to tray by default
sanitized shell lifecycle diagnostics enabled

Production business Widgets:
not stored in this repository
```

`DIP` means device-independent pixel: the Windows logical-pixel unit that remains stable across display scaling.

## Repository boundary

This repository contains the stable CoreHost platform only:

```text
application lifecycle
Windows tray host
global hotkey
startup registration
notifications
durable settings
window placement
system-policy coordination
diagnostics and migration
Widget contracts
Widget SDK
Widget package installer
Widget Manager
Universal Widget Framework
sample and regression fixtures
public CoreHost API documentation
```

Production Widget specifications, prototypes and implementations belong in the separate repository:

```text
kairosrepublica/kr-desktop-hub-widgets
```

CoreHost may retain sample Widgets and regression fixtures because they prove the host contract. CoreHost must not absorb business Widget logic.

## Public release history

The GitHub Releases page is the canonical public distribution record. Historical pre-releases are retained because they document meaningful engineering checkpoints rather than artificial version inflation.

| Release | Public checkpoint | Core engineering result |
|---|---|---|
| `v0.1.0-rc1` | Portable release candidate | Self-contained Windows build, clean extraction and executable self-test |
| `v0.2.1-rc1` | Public platform freeze | Repository boundary, public roadmap and atomic-checkpoint governance |
| `v1.0.0-alpha.1` | Universal Widget Framework | Shared host UI tokens, layout controller and brokered capability foundation |
| `v1.1.0-rc1` | Discovery-free state transitions | Collapse, Expand, enable, disable and order changes no longer rediscover packages |
| `v1.2.0-rc1` | Transactional installed catalog | Candidate discovery, acceptance and exact reconciliation are separated |
| `v1.3.0-rc1` | Windows shell release candidate | Non-disruptive popup policy and sanitized shell diagnostics |
| `v1.4.0-rc1` | Default KR icon | Executable, popup and tray branding |
| `v1.5.0-rc1` | Portable-helper hardening | Dynamic solution and smoke-test discovery, path anchoring and expanded Git status |
| `v2.0.0` | Delivered CoreHost | Stable portable platform and independent Widgets-repository boundary |

## Development and debugging history

The public record keeps a sanitized history of the important engineering iterations. Private logs, credentials, machine-specific paths and internal failure transcripts remain outside GitHub.

### Foundation

| Checkpoint | What changed |
|---|---|
| Contracts and abstractions | Added platform-neutral contracts and Windows composition seams |
| Core runtime | Added runtime foundation, scheduler, quarantine and system-resource governance |
| Desktop integration | Added tray shell, global hotkey, window placement and durable settings |
| Diagnostics and SDK | Added diagnostics migration and Widget SDK |
| Portable workflow | Added self-contained Windows packaging and clean-extraction validation |

### Widget-platform expansion

| Checkpoint | What changed |
|---|---|
| Package installer | Added validated internal `.krwidget.zip` installer foundation |
| Widget Manager | Added explicit Owner-controlled inventory, enable, disable and ordering workflow |
| Capability governance | Added deny-by-default broker contracts |
| Universal Widget Framework | Added shared UI tokens, layout controller, collapse state and tray-icon broker |
| Installed catalog | Added runtime manifest adapter and installed-package discovery |
| Host composition | Wired Widget Management UI into persistent Windows host composition |

### Stabilization and debug iterations

| Symptom or risk | Root cause | Public correction |
|---|---|---|
| Tray tooltip corruption | Unsafe non-ASCII tooltip output | Replaced with stable ASCII status text |
| Quiet-hours clearing failed | Clearing semantics were not closed end-to-end | Added explicit clearing, reload and validation coverage |
| Collapse changed outer popup geometry | Widget card state was coupled to outer viewport resizing | Locked Owner-sized viewport and routed overflow through host scrolling |
| Collapse / Expand triggered degraded-catalog rejection | State-only transitions were coupled to filesystem discovery | Added discovery-free state projection and targeted card reconciliation |
| Catalog candidate could mutate host state before acceptance | Discovery and accepted state were not separated | Added pure staging, acceptance gate and exact reconciliation |
| Shell popup disturbed taskbar or input-method-editor behavior | Ordinary popup activation was too intrusive | Added non-disruptive popup policy, tray-only minimize and sanitized lifecycle diagnostics |
| Default Windows icon was generic | Branding was not bound to executable, popup and tray | Added multi-size KR icon assets and governed fallback |
| Portable helper assumed a fixed solution or brittle path shape | Release tooling depended on static repository assumptions | Added dynamic solution discovery, dynamic smoke-test discovery, process working-directory anchoring and expanded Git status |

For the complete sanitized engineering record, see:

```text
docs/history/KR_Desktop_Hub_CoreHost_Public_Development_And_Debug_History.md
```

## GitHub release governance

A validated externally meaningful checkpoint must not stop at a pushed commit. It must also publish or update the corresponding GitHub tag and Release promptly.

See:

```text
docs/governance/PUBLIC_DEVELOPMENT_RECORD_POLICY.md
docs/governance/PUBLIC_RELEASE_AND_ITERATION_HISTORY_POLICY.md
```

## Start here

For ordinary repository checkpoint work:

```powershell
.\START_HERE.ps1
```

For portable release generation:

```powershell
.\tools\BUILD_VERIFY_PORTABLE_RELEASE.ps1 -Version "2.0.0"
```

## CoreHost v2.0.0 documentation

```text
docs\release\KR_Desktop_Hub_CoreHost_v2.0.0_Release_Notes.md
docs\release\KR_Desktop_Hub_CoreHost_v2.0.0_Manual_Acceptance_Checklist.md
docs\maintenance\KR_Desktop_Hub_CoreHost_Maintainer_Handoff_v2.0.0.md
docs\maintenance\KR_Desktop_Hub_CoreHost_AI_CoCoder_Public_Instructions_v2.0.0.md
```

## Widget developer entry point

Future Widget developers should begin with:

```text
docs\api\KR_Desktop_Hub_CoreHost_Widget_Developer_API_v2.0.0.md
docs\api\KR_Desktop_Hub_CoreHost_API_Surface_Map_v2.0.0.md
docs\architecture\KR_Desktop_Hub_CoreHost_And_Widgets_Repository_Separation_v1.0.md
```

## Security and governance

Never commit secrets, private logs, local configuration, personal calendar data, tokens, private certificates, private Owner instructions or machine-specific private paths.

Every validated engineering step must create one honest scoped commit, push immediately, verify `origin/main`, update the public narrative and publish the corresponding GitHub Release when the checkpoint is externally meaningful.

## Explicit exclusions

This repository does not contain:

```text
KR World Time-Space production implementation
KR Trading Clock production implementation
market-session business logic
holiday packs
weather business logic
calendar business logic
reminder business logic
third-party Widget marketplace
cloud backend
automatic online updater
```

## License

No open-source license has been selected yet.

A public portfolio repository and a permission grant for third-party reuse are separate decisions.
