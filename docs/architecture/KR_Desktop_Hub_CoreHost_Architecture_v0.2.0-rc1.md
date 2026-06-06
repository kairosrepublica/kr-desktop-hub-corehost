# KR Desktop Hub CoreHost Architecture

## Release

`v0.2.0-rc1`

## Purpose

KR Desktop Hub CoreHost is a Windows desktop host for governed local Widgets. It provides a durable host lifecycle, tray integration, panel state, settings, diagnostics, migration, Widget package installation, capability governance and extension boundaries.

## Project layers

```text
KRDesktopHub.Contracts
KRDesktopHub.Platform.Abstractions
KRDesktopHub.Core
KRDesktopHub.Platform.Windows
KRDesktopHub.App.Windows
KRDesktopHub.WidgetSdk
```

The Windows application project is the composition root. It connects Core services with Windows-specific adapters without forcing lower-level projects to depend on higher-level orchestration layers.

## Implemented CoreHost capabilities

```text
Windows tray lifecycle
panel show and hide
window-placement persistence
durable CoreHost settings
Settings Center
hotkey conflict diagnostics
notification governance
system-policy handling
resource governance
diagnostics export
sensitive-field redaction
migration import and export
safe ZIP extraction
pre-import backup
Widget manifest validation
Widget package installation
atomic install and rollback
Widget Manager
capability governance
broker contracts
Widget SDK sample
portable self-test mode
```

## Widget capability boundary

Enabled brokered capabilities:

```text
clock.read
notification.send
```

Reserved but disabled capabilities:

```text
network.http
calendar.read
file.read.scoped
file.write.scoped
```

Prohibited capabilities:

```text
shell.execute
script.execute
```

## Package installation boundary

Production Widget packages use:

```text
.krwidget.zip
```

Development-only folder installation remains explicitly separate. Dropped files are never executed automatically. The installer stages, validates, extracts safely, checks compatibility and capabilities, installs atomically and supports rollback.

## Settings Center boundary

The Settings Center exposes stable keys, recommended defaults, recommendation reasons and application modes:

```text
Immediate
RestartRequired
ReservedForFutureBinding
```

A Windows composition-root bridge synchronizes UI-facing settings with the active runtime settings store.

## Release validation

The release runner performs:

```text
full solution build
all discovered SmokeTests console projects
self-contained Windows publish
clean ZIP extraction
extracted EXE self-test
SHA-256 sidecar generation
```


## Universal Widget framework extension

The next CoreHost foundation adds:

```text
600 DIP default popup width
adaptive Widget height controller
collapsed, expanded and disabled host state
CoreHost-owned UI design tokens
floating-dialog request broker
tray-icon request broker
approved Windows tray visual-state mapping
```

The extension remains generic. Business logic stays inside isolated Widget packages.
