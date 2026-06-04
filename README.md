# KR Desktop Hub CoreHost

Portable-first and extensible desktop CoreHost for Windows 11 Widgets.

## Current status

```text
Project stage: Batch 0 — Repository Bootstrap and Execution Control
Runnable application: not implemented yet
Production Widgets: intentionally deferred
Final panel UI: intentionally unfrozen
```

This repository establishes the public, sanitized engineering baseline for **KR Desktop Hub CoreHost**.

The CoreHost will provide:

- application lifecycle;
- single-instance execution;
- system-tray lifecycle;
- global hotkeys;
- Windows notifications;
- configurable startup behavior;
- localization interfaces;
- configuration and state management;
- resource monitoring;
- Widget discovery, scheduling, health monitoring, quarantine, diagnostics and rollback.

Future user-facing functions must be developed as independent Widgets rather than embedded inside CoreHost business logic.

## Target platform

Initial implementation target:

```text
Windows 11 x64
```

Planned compatibility paths are reserved for:

```text
Windows ARM64
Apple macOS
```

Those platforms are **not implemented or supported yet**.

## Distribution principle

Portable ZIP first:

```text
extract
run
```

A simple installer may be added later. Portable mode must remain available.

## Start here

Run:

```powershell
.\START_HERE.ps1
```

Only `START_HERE.ps1` is exposed as the normal operator entry point. Advanced scripts remain under `tools\advanced\`.

## Documentation

Start with:

```text
docs\Product_Scope.md
docs\Architecture.md
docs\ROADMAP_IMPLEMENTATION.md
docs\governance\PUBLIC_DEVELOPMENT_RECORD_POLICY.md
```

## Security

Never commit secrets, private logs, personal calendar data, local configuration, API keys, tokens, private certificates or machine-specific private paths.

See:

```text
SECURITY.md
```

## License

No open-source license has been selected yet.

A public portfolio repository and a permission grant for third-party reuse are separate decisions.


## Canonical GitHub repository

The single canonical public repository target is:

```text
kairosrepublica/kr-desktop-hub-corehost
```

Commits are authored through the Kent Reis personal GitHub identity:

```text
kentreis
```

Do not create a competing primary repository under the personal account.

## Portable release candidate

Build and validate the local Windows 11 x64 portable release candidate:

```powershell
.\tools\BUILD_VERIFY_PORTABLE_RELEASE.ps1
```

The generated ZIP, SHA-256 file and resource baseline remain under:

```text
dist/releases/
```

Release binaries are local artifacts until manual desktop acceptance is complete.
