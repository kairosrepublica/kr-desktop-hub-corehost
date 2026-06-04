# KR Desktop Hub Widget API â€” Batch 1 Baseline

## Purpose

This document defines the first platform-neutral Widget contract baseline.

## Core lifecycle

Each Widget implements:

```text
IKrWidget
InitializeAsync
StartAsync
PauseAsync
ResumeAsync
StopAsync
```

## Host-provided context

Each Widget receives controlled access through:

```text
IWidgetContext
IWidgetLogger
IWidgetScheduler
IWidgetStateStore
IWidgetSettingsStore
IEventBus
ICommandRegistry
IClock
ILocalizationService
IWidgetNotificationClient
```

## Runtime modes

```text
AlwaysOn
OnDemand
ScheduledWindow
PeriodicRun
EventTriggered
ManualOnly
```

## Rule

Widgets depend on Contracts. CoreHost must not depend on concrete Widgets.

## Status

Batch 1 baseline. Future additions must remain backward-aware.