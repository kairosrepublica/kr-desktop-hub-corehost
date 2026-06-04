# KR Desktop Hub Platform Abstractions API â€” Batch 1 Baseline

## Purpose

Windows-specific behavior must remain behind platform interfaces so the CoreHost can later support Windows ARM64 and a separate macOS shell.

## Interfaces

```text
ITrayService
IGlobalHotkeyService
ISystemNotificationService
IStartupRegistrationService
IPanelWindowService
IPowerStateService
INetworkStateService
IPrivilegeService
IPlatformInfoService
```

## Rule

No WPF, Win32 or macOS-specific type may leak into platform-neutral Contracts.

## Status

Batch 1 baseline. Windows implementations are deferred to the Windows adapter batch.