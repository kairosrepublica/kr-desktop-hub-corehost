# KR Desktop Hub System Policies API â€” Batch 5 Baseline

## Purpose

Batch 5 adds low-overhead system-event handling and configurable resource governance.

## Windows event adapters

```text
WindowsPowerStateService
WindowsNetworkStateService
WindowsSessionStateService
WindowsTimeZoneChangeService
WindowsProcessResourceMonitorService
```

## Core policy services

```text
CoreHostPolicyOptions
WidgetWorkloadProfile
SystemPolicyState
WidgetPolicyDecision
SystemPolicyEvaluator
SystemPolicyCoordinator
SystemPolicySignal
```

## Default behavior

```text
refresh only stale active Widgets after resume
do not replay every missed scheduled run
pause network-heavy Widgets while locked
pause low-priority Widgets on battery
refresh time-sensitive Widgets after time-zone change
debounce retries after network recovery
stop meaningless visual refresh while panel is hidden
stop network requests while a Widget is inactive
sample CPU and working-set memory at low frequency
```

## Resource thresholds

Idle CPU and memory warning thresholds remain nullable until the Proof-of-Concept baseline is measured.

This avoids inventing arbitrary acceptance numbers before observing the real application.

## Windows event notes

The WPF shell has a message pump, allowing `SystemEvents` notifications to reach the application.

## Status

Batch 5 baseline.