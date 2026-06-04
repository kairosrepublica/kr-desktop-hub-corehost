# KR Desktop Hub CoreHost â€” Portable Manual Acceptance Checklist

## Purpose

Run this checklist after automated portable validation succeeds.

## Clean-machine check

```text
extract the portable ZIP on a Windows 11 x64 computer
run RUN_SELF_TEST.cmd
confirm the JSON marker reports PASS
run START_KR_DESKTOP_HUB.cmd
confirm the tray icon appears
```

## Interaction check

```text
double-click tray icon and confirm panel show or hide
press Ctrl+Alt+K and confirm panel show or hide
close panel and confirm process remains in tray
open tray menu and send test notification
toggle login startup and confirm notification
exit from tray menu and confirm process terminates
```

## Resource check

```text
leave tray host idle with panel hidden
observe CPU and memory in Task Manager
compare observations with the generated baseline JSON
do not freeze thresholds from a single measurement
```

## Status

This checklist is intentionally manual because tray interaction depends on the real desktop session.