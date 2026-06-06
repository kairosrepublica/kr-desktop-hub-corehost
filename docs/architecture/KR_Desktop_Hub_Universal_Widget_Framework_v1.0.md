
# KR Desktop Hub Universal Widget Framework

## Version

```text
v1.0 foundation
```

## Purpose

The CoreHost owns one universal Widget framework. Business Widgets remain isolated packages and consume only versioned CoreHost contracts.

## Default popup width

```text
600 DIP
```

Widgets follow the width assigned by CoreHost. `600 DIP` is the normal design target, not a private hard-coded window width inside each Widget.

## Adaptive height

Every Widget registers:

```text
preferred expanded height
minimum collapsed height
default enabled state
default collapsed state
order
```

Every rendered Widget may report a measured desired height.

CoreHost calculates:

```text
expanded:
measured desired height

collapsed:
minimum collapsed height

disabled:
zero visible height
```

When the combined Widget height exceeds the desktop work area, CoreHost applies host-level scrolling. Ordinary Widget growth must not create nested internal vertical scrollbars.

## Shared UI boundary

The Windows composition root owns `WidgetUiResources.xaml`.

This shared layer contains visual tokens only:

```text
surface colors
border colors
status colors
spacing
corner radius
header text style
secondary text style
```

A Widget must not import another Widget's styles, code, settings or mutable runtime state.

## Capabilities

New brokered framework capabilities:

```text
ui.surface
height.report
settings.persist
state.persist
context-menu.register
dialog.request
tray-icon.request
diagnostics.write
```

Capabilities remain deny-by-default at runtime.

## Floating dialogs

Widgets request CoreHost-owned floating dialogs through `dialog.request`.

The CoreHost owns desktop-safe presentation so dialogs are not clipped by Widget bounds.

## Tray icons

Widgets request approved declarative tray-icon states through `tray-icon.request`.

CoreHost owns:

```text
approved state registry
priority ceilings
expiry
arbitration
fallback
platform icon mapping
```

Widgets cannot submit arbitrary icon-file paths.

## Phase 1 application

```text
KR World Time-Space:
preferred expanded height 220 DIP
minimum collapsed height 44 DIP

KR Trading Clock:
preferred expanded height 500 DIP
minimum collapsed height 44 DIP
```
