# KR Desktop Hub Checkpoint 2 Activation Boundary

## Purpose

The Widget installer and Widget runtime historically consumed adjacent but different manifest shapes.

```text
installer:
snake_case package manifest

runtime loader:
camelCase runtime manifest
```

The explicit `InstalledWidgetManifestAdapter` prevents each production Widget from solving this problem independently.

## Installed package path

```text
<CoreHost data root>
plugins
installed
<widget_id>
manifest.json
lib
<entry assembly>.dll
```

## Translation

Schema-1 package manifests map into runtime manifests with backward-compatible defaults:

```text
display_name:
widget_id when omitted

required_contracts_version:
1.0.0 when omitted

activation_mode:
OnDemand when omitted

presentation metadata:
preserved from package manifest
```

## Catalog

The installed catalog scans only top-level installed Widget directories.

It reports:

```text
Widget ID
display name
package version
installed path
declared capabilities
enabled or disabled
collapsed or expanded
order
preferred expanded height
minimum collapsed height
discovery failures
layout snapshot
```

## Separation of checkpoints

Checkpoint 2A freezes backend activation and inventory.

Checkpoint 2B wires:

```text
Widget Management inventory UI
production WidgetHostSurface composition
floating-dialog presenter
Windows tray broker application
```
