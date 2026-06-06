# KR Desktop Hub CoreHost State-Only Widget-Host Transitions v1.0

## Status

```text
CoreHost stabilization checkpoint
Production business Widgets remain blocked
```

## 1. Purpose

CoreHost now separates generic Widget-host state mutations from installed-package filesystem discovery.

The following operations are state-only transitions:

```text
collapse
expand
enable
disable
move up
move down
host-state synchronization
```

They update the accepted in-memory Widget-host projection and reconcile visible cards without rediscovering installed package directories.

## 2. Discovery trigger boundary

Installed-package discovery remains reserved for explicit lifecycle events:

```text
startup
manual Refresh Installed Widgets
successful package install
successful package uninstall
explicit recovery workflow
```

A normal Collapse or Expand action must not enter degraded catalog-snapshot evaluation.

## 3. Universal Widget-chrome seam

Reusable Widget chrome now has a framework-owned transition seam:

```text
WidgetHostChromePresentation
WidgetHostChromeTransitionController
```

The sample Widget card consumes this seam as the canonical regression fixture.

## 4. Visual reconciliation

CoreHost preserves existing card and visual-surface instances during state-only updates.

It reconciles visible cards by Widget ID and reorders existing cards when required.

## 5. Geometry contract

```text
default outer popup width:
600 DIP

minimum outer popup width:
600 DIP

minimum outer popup height:
240 DIP
```

Widget-host actions do not resize the outer popup. Stacked overflow remains a CoreHost-level scrolling responsibility.

## 6. Remaining stabilization work

This checkpoint does not declare CoreHost delivered.

Remaining work includes:

```text
transactional staged catalog discovery and accepted-catalog commit
exact active-registration and capability-approval pruning
framework LayoutChanged host subscription
popup Show / Hide activation policy
Microsoft Pinyin indicator replay
popup lower-shadow and tray-region darkening replay
sanitized shell diagnostics
Owner manual acceptance
```
