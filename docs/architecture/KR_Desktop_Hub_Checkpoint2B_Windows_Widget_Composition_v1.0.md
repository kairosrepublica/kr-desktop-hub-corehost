
# KR Desktop Hub — Checkpoint 2B Windows Widget Composition

## Decision

CoreHost owns the popup panel, Widget Management, layout state, floating dialogs and tray icon.

Widgets remain isolated packages.

## Data flow

```text
plugins/installed
InstalledWidgetCatalogService
InternalWidgetManagerService
InstalledWidgetHostCompositionCoordinator
MainWindow.WidgetHostSurface
WidgetHostCard
```

## State flow

```text
Enable / Disable
Collapse / Expand
Order
JsonWidgetHostStateStore
WidgetHostLayoutController
```

## Visual content

CoreHost renders a generic card for each enabled installed Widget.

A governed Windows registry provides the visual-surface seam. Concrete Widget packages attach content later without importing code from another Widget.

## Dialogs

Widgets request a declarative dialog. CoreHost creates a floating `WidgetDialogWindow` owned by the main panel.

## Tray icon

Widgets request an approved state key. `GovernedWidgetTrayIconBroker` arbitrates. `WindowsTrayService` remains the only owner of the Windows notification-area icon.

## Security boundary

Checkpoint 2B does not execute arbitrary files dropped into `plugins/inbox`. Installation remains explicit. Visual-surface registration remains a governed integration action.
