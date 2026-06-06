# KR Desktop Hub CoreHost Stabilization — Owner-Sized Viewport and Widget Refresh Integrity v1.0

## Purpose

This checkpoint supersedes the not-yet-applied alpha5 shell-geometry policy and preserves its refresh-integrity protections.

## Binding popup geometry policy

```text
default width:
600 DIP

minimum width:
600 DIP

Owner actions:
may widen the popup
may adjust popup height

Widget Expand / Collapse:
must not change outer popup width
must not change outer popup height

Widget refresh:
must not reset Owner-adjusted geometry

content overflow:
use CoreHost host-level vertical scrolling
do not auto-grow or auto-shrink the outer popup
```

## Why the policy changed

Earlier drafts used `preserve-or-grow` behavior. That still allowed Widget content changes to mutate outer popup geometry.

The Owner has frozen a stricter boundary:

```text
outer popup geometry:
Owner-controlled

child Widget layout:
CoreHost-managed inside the viewport
```

## Refresh-integrity protections retained from alpha5

```text
serialize installed-catalog refreshes
serialize host mutations
retain last known-good snapshot
reject degraded temporary snapshots
build next Widget cards before replacing visible cards
publish governed failure notifications
```

## Required regression gates

```text
MinWidth = 600 DIP
no Widget-driven Width assignment
no Widget-driven Height assignment
Expand does not resize outer popup
Collapse does not resize outer popup
overflow enables scrolling
vertically snapped shell remains vertically snapped
rapid Widget operations remain serialized
degraded refresh cannot clear last known-good panel
explicit Disable remains valid
existing 16 smoke-test projects
clean-extraction win-x64 EXE self-test
Owner manual acceptance stress replay
```
