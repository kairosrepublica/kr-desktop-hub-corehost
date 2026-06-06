# KR Desktop Hub Installed Widget Catalog API

## Scope

This API freezes the backend boundary between installed Widget packages and runtime activation.

## Core services

```text
InstalledWidgetManifestAdapter
InstalledWidgetCatalogService
```

## Catalog records

```text
InstalledWidgetCatalogItem
InstalledWidgetCatalogFailure
InstalledWidgetCatalogSnapshot
```

## Package-to-runtime translation

Installed production packages continue to use the strict snake_case package manifest.

The runtime loader also continues to support legacy camelCase development manifests.

`InstalledWidgetManifestAdapter` inspects the root manifest shape:

```text
manifest_schema_version present:
read WidgetPackageManifest
map into WidgetManifest

manifest_schema_version absent:
read legacy WidgetManifest directly
```

## Backend host-state controls

```text
RefreshInstalledWidgetsAsync
SetInstalledWidgetEnabled
SetInstalledWidgetCollapsed
SetInstalledWidgetOrder
GetInstalledWidgetLayout
```

These controls update the generic CoreHost host-state layer.

The next Windows composition checkpoint connects them to the Widget Management interface and the production `WidgetHostSurface`.

## Security boundary

```text
No automatic execution of dropped files.
No direct Widget ownership of tray icons.
No direct Widget ownership of desktop dialogs.
No business-Widget code in CoreHost.
No inter-Widget imports.
```
