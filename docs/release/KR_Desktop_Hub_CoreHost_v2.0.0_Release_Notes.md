# KR Desktop Hub CoreHost v2.0.0 Release Notes

## Release identity

```text
version:
2.0.0

target:
Windows 11 x64

distribution:
portable ZIP

deployment mode:
self-contained single-file application inside portable folder

repository:
kairosrepublica/kr-desktop-hub-corehost
```

## CoreHost platform delivered

```text
default branded KR CoreHost icon
```

```text
single-instance Windows tray host
global panel-toggle hotkey
startup registration
durable settings
Settings Center
window-placement persistence
Owner-controlled popup viewport
host-level overflow scrolling
notification governance
system-policy handling
resource monitoring
diagnostics export
portable data migration
Widget contracts
Widget SDK
Widget package installer
Widget Manager
Universal Widget Framework
state-only discovery-free transitions
transactional installed-catalog refresh
exact accepted registration reconciliation
capability pruning
tray-request revocation
non-disruptive popup show policy
sanitized shell lifecycle diagnostics
```

## Windows shell policy

```text
ShowActivated = false
ShowInTaskbar = false
ordinary Show does not force Activate()
```

The popup remains manually interactive when selected by the Owner.

## CoreHost and Widget separation

Production business Widgets are not included in the CoreHost repository.

They belong in:

```text
kairosrepublica/kr-desktop-hub-widgets
```

CoreHost retains only SDK examples and regression fixtures.

## Portable ZIP contents

```text
self-contained win-x64 application
START_KR_DESKTOP_HUB.cmd
RUN_SELF_TEST.cmd
configuration examples
resources
HelloWidget sample
release manifest
release notes
manual acceptance checklist
Widget developer API overview
CoreHost API surface map
```

## Manual acceptance required before final GitHub release

Run:

```text
KR_Desktop_Hub_CoreHost_v2.0.0_Manual_Acceptance_Checklist.md
```

Do not publish the final GitHub `v2.0.0` release until shell visual replay passes.
