# KR Desktop Hub CoreHost

> CoreHost v2.0.0 release-candidate status: the platform boundary is complete, state-only Widget-host transitions are discovery-free, installed-catalog refresh is transactional, and the Windows shell now uses a non-disruptive popup activation policy. Production business Widgets live in a separate repository and remain blocked until Owner manual shell acceptance and the final `v2.0.0` release checkpoint.

Portable-first, extensible desktop CoreHost for Windows 11 Widgets.

## Current status

```text
Project stage:
CoreHost v2.0.0 release candidate

Target platform:
Windows 11 x64

CoreHost popup:
Owner-controlled viewport
600-DIP default width
600-DIP minimum width
240-DIP minimum height
host-level overflow scrolling

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

## Start here

For ordinary repository checkpoint work:

```powershell
.\START_HERE.ps1
```

For portable release generation:

```powershell
.\tools\BUILD_VERIFY_PORTABLE_RELEASE.ps1 -Version "2.0.0"
```

## CoreHost v2.0.0 release documentation

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

Every validated engineering step must create one honest scoped commit, push immediately and verify `origin/main`.

See:

```text
SECURITY.md
docs\governance\PUBLIC_DEVELOPMENT_RECORD_POLICY.md
```

## Portable distribution principle

Portable ZIP remains the primary distribution format:

```text
extract
run START_KR_DESKTOP_HUB.cmd
```

The release package contains:

```text
self-contained win-x64 application
configuration examples
sample HelloWidget
release manifest
self-test launcher
manual acceptance checklist
Widget-facing API overview
```

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
