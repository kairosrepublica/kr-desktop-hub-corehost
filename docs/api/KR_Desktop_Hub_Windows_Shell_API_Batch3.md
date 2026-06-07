# KR Desktop Hub Windows Shell API — v2.0.0

## Purpose

Expose the Windows shell implementation boundary and the non-disruptive popup policy used by CoreHost `v2.0.0`.

## Implemented services

```text
WindowsTrayService
WindowsTrayBalloonNotificationService
WindowsGlobalHotkeyService
WindowsStartupRegistrationService
WindowsPrivilegeService
WindowsPlatformInfoService
WindowsWindowPlacementService
```

## Panel shell policy

```text
CoreHostPanelShellPolicy.ShowActivated = false
CoreHostPanelShellPolicy.ShowInTaskbar = false
CoreHostPanelShellPolicy.ForceActivateAfterOrdinaryShow = false
```

Ordinary CoreHost popup Show:

```text
shows the popup if hidden
synchronizes system-policy visibility
does not force Activate()
writes sanitized shell lifecycle diagnostics
```

Manual Owner interaction remains available when the popup is clicked.

## WPF application behavior

```text
single-instance process guard
hidden panel by default after login
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

## Diagnostics

Shell lifecycle records are written through the governed structured file logger.

Category:

```text
shell.panel.lifecycle
```

Fields:

```text
action
reason
previous visibility
current visibility
active state
focused-element type
popup bounds
working-area bounds
Topmost
ShowActivated
ShowInTaskbar
```

Diagnostics are sanitized and exportable through the existing CoreHost diagnostics path.

## Manual acceptance boundary

Automated tests cannot prove taskbar-region visuals or Microsoft Pinyin indicator behavior.

Run:

```text
../release/KR_Desktop_Hub_CoreHost_v2.0.0_Manual_Acceptance_Checklist.md
```

## Deferred stronger fallback

Do not apply `WS_EX_NOACTIVATE` casually.

A stronger Win32 fallback may be evaluated only if Owner manual replay proves the lower-risk WPF policy insufficient.
