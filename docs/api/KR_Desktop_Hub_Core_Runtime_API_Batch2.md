# KR Desktop Hub Core Runtime API â€” Batch 2 Baseline

## Purpose

Batch 2 adds platform-neutral runtime services.

## Runtime services

```text
SystemClock
ConsoleWidgetLogger
InMemoryEventBus
CommandRegistry
EnvironmentPathResolver
JsonConfigurationLoader
JsonWidgetStateStore
JsonWidgetSettingsStore
JsonLocalizationService
CoreRuntimeFactory
```

## Boundaries

```text
Core Runtime contains no Windows tray implementation.
Core Runtime contains no WPF type.
Core Runtime contains no market-session logic.
Core Runtime contains no concrete Widget dependency.
```

## Storage

The public example configuration uses:

```text
%LOCALAPPDATA%\KR\KRDesktopHub
```

Owner-specific paths remain local configuration only.

## Status

Batch 2 baseline. Windows-specific adapters are implemented later.