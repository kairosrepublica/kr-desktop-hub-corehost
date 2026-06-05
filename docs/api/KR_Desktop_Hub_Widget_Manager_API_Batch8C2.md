# KR Desktop Hub Internal Widget Manager API

## Batch identity

`Stabilization Batch 8C2`

## Purpose

This batch connects the validated internal Widget package-installer foundation to an explicit Owner-controlled CoreHost interface.

It does not create or execute any production Widget.

## Owner-controlled entry points

The Widget Manager can be opened from:

- the system-tray menu;
- the CoreHost panel.

## Explicit install routes

### `Refresh Inbox`

Lists eligible `.krwidget.zip` files in:

```text
plugins/inbox/
```

Refresh is inert. It never installs or executes a dropped file.

### `Install Selected Inbox Package`

Installs only the explicitly selected top-level inbox package.

### `Choose Package File`

Uses the Windows file picker to select an Owner-approved `.krwidget.zip` package from any folder.

Selection alone is inert. Installation starts only after the Owner clicks:

```text
Install Chosen Package File
```

### `Advanced development-only install`

Folder installation is disabled by default.

For one explicit development action, the Owner must enable the Advanced checkbox and choose a folder through the Windows folder picker. The checkbox resets after the action.

## Capability boundary

The current Widget Manager initializes its production installer with an empty capability allowlist.

This is deliberate. Production Widgets are not yet enabled. A later CoreHost stabilization batch must define capability governance and brokered platform access before production Widget development begins.

## Safety boundary

The Widget Manager:

- does not auto-install inbox files;
- does not auto-run inbox files;
- does not execute embedded scripts;
- does not dynamically load installed Widget assemblies;
- does not begin production Widget development.