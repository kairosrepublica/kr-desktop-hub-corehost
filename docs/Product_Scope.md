# Product Scope

## Product definition

KR Desktop Hub CoreHost is a lightweight, persistent Windows 11 desktop host for isolated Widgets.

It is infrastructure, not a bundle of embedded business modules.

## CoreHost responsibilities

```text
application lifecycle
single-instance execution
system tray
global hotkey
startup configuration
durable configuration persistence
window-placement persistence
Owner-controlled popup geometry
shell show / hide lifecycle
non-disruptive popup activation policy
host-level overflow scrolling
Windows notifications
resource monitoring
system-policy coordination
Widget discovery
transactional installed-catalog refresh
Widget package installation
Widget Management
Widget-host state persistence
capability governance
floating-dialog broker
tray-icon broker
diagnostics
migration
rollback
public Widget-facing API documentation
```

## Widget responsibilities

Production Widgets live in the separate repository:

```text
kairosrepublica/kr-desktop-hub-widgets
```

Examples:

```text
world clocks
market sessions
holiday packs
calendar
reminders
personal plans
weather
financial calendar
future user-facing modules
```

## Allowed CoreHost Widget content

CoreHost may retain only:

```text
sample Widgets
test fixtures
contract examples
SDK examples
```

These prove platform behavior and must not become business modules.

## Explicitly deferred

```text
third-party Widget marketplace
cloud backend
automatic online updater
live DLL replacement
macOS implementation
Windows ARM64 release artifact
stronger Win32 no-activate fallback unless manual shell replay proves it necessary
```
