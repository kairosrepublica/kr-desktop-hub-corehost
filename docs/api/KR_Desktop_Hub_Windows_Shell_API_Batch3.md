# KR Desktop Hub Windows Shell API â€” Batch 3 Baseline

## Purpose

Batch 3 adds the first runnable Windows desktop shell.

## Implemented services

```text
WindowsTrayService
WindowsTrayBalloonNotificationService
WindowsGlobalHotkeyService
WindowsStartupRegistrationService
WindowsPrivilegeService
WindowsPlatformInfoService
```

## WPF application behavior

```text
single-instance process guard
hidden panel by default
Ctrl+Alt+K panel toggle
tray double-click panel toggle
close button hides panel
tray Exit command terminates process
tray menu toggles current-user login startup
tray-balloon test notification
```

## Startup registration

The Windows adapter writes the current-user Run key and includes:

```text
--start-hidden
--startup-delay-seconds 10
```

## Notification boundary

Batch 3 implements the lightweight tray-balloon fallback.

A future batch may add modern Windows Notification Center transport without changing the Widget-facing notification contract.

## Visual boundary

The panel remains a placeholder. Final visual design is intentionally unfrozen.