
# KR Desktop Hub — Checkpoint 2B Windows Widget Composition API v1.0

## Scope

This checkpoint connects the installed Widget catalog to the Windows CoreHost shell.

## Windows composition classes

```text
InstalledWidgetHostCompositionCoordinator
WindowsInstalledWidgetVisualSurfaceRegistry
WindowsWidgetFrameworkServices
WidgetHostCard
WidgetDialogWindow
WindowsWidgetDialogPresenter
```

## Manager controls

```text
Refresh installed inventory
Enable
Disable
Expand
Collapse
Move Up
Move Down
```

Disabled Widgets disappear from the popup panel and remain reopenable through Widget Management.

## Host behavior

```text
default popup width:
600 DIP

height:
computed from enabled Widget host snapshots

desktop overflow:
host-level ScrollViewer fallback
```

## Visual-surface isolation seam

CoreHost owns the generic card frame and layout.

Concrete Widget packages provide their visual content through a governed Windows visual-surface registration seam. Checkpoint 2B does not auto-run arbitrary files dropped into `plugins/inbox`.

## Floating dialogs

Widgets request declarative dialog models through:

```text
IWidgetDialogBroker
```

CoreHost presents the floating WPF dialog above the full panel. The dialog is not clipped by an individual Widget boundary.

## Tray icons

Widgets submit declarative requests through:

```text
IWidgetTrayIconBroker
```

CoreHost arbitrates approved state keys, priorities and expiry. The Windows tray service applies only approved visual states.

## Integrated context seam

The optional interface:

```text
IWidgetHostIntegrationContext
```

extends the base Widget context with:

```text
HostLayout
Dialogs
TrayIcons
```

Existing Widgets remain compatible with the original `IWidgetContext`.
