
# KR Desktop Hub Universal Widget Framework API

## Contracts

```text
WidgetPresentationMetadata
WidgetHostRegistration
WidgetHostSurfaceSnapshot
WidgetHostLayoutSnapshot
IWidgetHostLayoutClient
WidgetDialogRequest
WidgetDialogResult
IWidgetDialogBroker
WidgetTrayIconRequest
WidgetTrayIconSelection
IWidgetTrayIconBroker
```

## Core services

```text
WidgetHostFrameworkDefaults
JsonWidgetHostStateStore
WidgetHostLayoutController
GovernedWidgetHostLayoutClient
GovernedWidgetDialogBroker
WidgetTrayIconStateDefinition
GovernedWidgetTrayIconBroker
```

## Windows UI design system

```text
src/KRDesktopHub.App.Windows/WidgetUiResources.xaml
```

## Windows tray visual states

The Windows adapter accepts only built-in mapped visual-state keys:

```text
corehost.default
corehost.information
corehost.warning
corehost.error
corehost.shield
```

Future Widget-specific icon states must be explicitly added to the CoreHost-owned registry with approved built-in assets.

## Security boundary

```text
No arbitrary icon-file paths.
No direct Widget ownership of NotifyIcon.
No direct Widget ownership of floating dialog windows.
No inter-Widget imports.
```
