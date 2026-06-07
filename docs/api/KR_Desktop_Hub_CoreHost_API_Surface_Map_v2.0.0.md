# KR Desktop Hub CoreHost API Surface Map v2.0.0

## Purpose

This map helps future AI co-coders and Widget developers locate the correct CoreHost boundary without importing implementation layers accidentally.

## Assembly map

| Assembly | Responsibility | Widget dependency allowed |
|---|---|---|
| `KRDesktopHub.Contracts` | Stable interfaces, manifests, broker contracts, framework contracts | Yes |
| `KRDesktopHub.WidgetSdk` | Widget helper layer and SDK examples | Yes |
| `KRDesktopHub.Platform.Abstractions` | Platform-neutral CoreHost adapters | No direct production Widget dependency unless explicitly approved |
| `KRDesktopHub.Platform.Windows` | Windows implementation details | No |
| `KRDesktopHub.Core` | Host runtime, package installer, catalog, governance, scheduler | No |
| `KRDesktopHub.App.Windows` | Windows Presentation Foundation shell and composition | No |

## Widget-facing contract map

| Surface | Primary type or document | Use |
|---|---|---|
| Widget entry point | `IWidget` | Widget lifecycle |
| Widget context | `IWidgetContext` | Governed services available to a Widget |
| State store | `IWidgetStateStore` | Widget-specific persistent state |
| Settings store | `IWidgetSettingsStore` | Widget-specific settings |
| Logger | `IWidgetLogger` | Sanitized diagnostic writing |
| Host layout | `IWidgetHostLayoutClient` | Desired internal height reporting |
| Floating dialog | broker contracts | Host-owned desktop-safe dialog request |
| Tray icon | broker contracts | Approved declarative tray state request |
| Manifest | schema v1.2 | Package declaration |
| Package installer | installer API | `.krwidget.zip` install path |
| Widget Management | manager API | Enable, disable, collapse, expand, ordering |

## CoreHost-only implementation map

| Surface | CoreHost type | Widget access |
|---|---|---|
| Transactional catalog staging | `InstalledWidgetCatalogService.DiscoverAsync` | Forbidden |
| Accepted catalog commit | `InstalledWidgetCatalogService.CommitAcceptedCandidate` | Forbidden |
| Runtime registration reconciliation | `WidgetHostLayoutController.ReconcileActiveRegistrations` | Forbidden |
| Accepted projection | `InstalledWidgetCatalogProjection` | Forbidden |
| Windows card composition | `InstalledWidgetHostCompositionCoordinator` | Forbidden |
| Windows popup shell | `MainWindow`, `App` | Forbidden |
| Shell diagnostics | `CoreHostPanelShellDiagnosticFormatter` and structured logger | Forbidden except exported sanitized evidence |

## State-only transition rule

The following Widget-host operations must remain discovery-free:

```text
Collapse
Expand
Enable
Disable
Move Up
Move Down
desired-height report
visual rerender
```

## Explicit refresh rule

Filesystem discovery is allowed only for:

```text
startup
manual Refresh
successful package install
successful package uninstall
explicit recovery
```

## Repository map

CoreHost:

```text
kairosrepublica/kr-desktop-hub-corehost
```

Production Widgets:

```text
kairosrepublica/kr-desktop-hub-widgets
```

The separate Widgets repository may pin a CoreHost API baseline, but it must not copy mutable CoreHost implementation code.

## Default CoreHost icon ownership

```text
EXE icon:
KRDesktopHub.App.Windows/Assets/CoreHost/KRDesktopHub.CoreHost.ico

WPF popup window icon:
MainWindow.xaml Icon binding

Tray default visual state:
WindowsTrayVisualStateCatalog.Default
resolved through
CoreHostDefaultIconCatalog.Resolve()
```

Production Widgets must not replace the CoreHost default icon directly. A Widget may request only approved declarative tray-icon states through the constrained CoreHost-owned broker.
