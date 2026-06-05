# KR Desktop Hub CoreHost v0.2.0-rc1

## Release type

Portable Windows x64 release candidate.

## Included

```text
CoreHost Windows tray application
durable settings
Settings Center
window-placement persistence
hotkey runtime reload
Widget package installer
Widget Manager
capability governance
diagnostics and migration
self-test mode
```

## Portable package

The portable artifact is generated as:

```text
dist/releases/KRDesktopHub_CoreHost_win-x64_portable_v0.2.0-rc1.zip
```

A SHA-256 sidecar is generated beside the ZIP.

## Validation model

The release process validates the complete solution, runs every discovered SmokeTests console project, extracts the generated ZIP into a clean directory and runs the extracted executable in self-test mode.

## Current scope

This release candidate freezes the CoreHost foundation. Production Widget development remains a separate follow-on stream.