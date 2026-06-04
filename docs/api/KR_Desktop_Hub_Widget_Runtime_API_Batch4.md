# KR Desktop Hub Widget Runtime API â€” Batch 4 Baseline

## Purpose

Batch 4 adds the platform-neutral Widget Runtime.

## Implemented services

```text
WidgetManifest
WidgetManifestValidator
WidgetPluginLoadContext
WidgetPluginLoader
WidgetDiscoveryResult
PeriodicWidgetScheduler
DefaultWidgetContext
WidgetRuntimePolicy
WidgetRuntimeController
WidgetRuntimeSnapshot
NullWidgetNotificationClient
```

## Plugin loading

The runtime loads internal Widgets through:

```text
AssemblyLoadContext
AssemblyDependencyResolver
manifest.json
```

Each plugin receives an isolated load context while sharing the Host Contracts assembly.

## Lifecycle

```text
Initialize
Start
Pause
Resume
Stop
```

## Runtime policy

Recommended defaults:

```text
max retries: 5
quarantine threshold: 5 failed cycles
operation timeout: 30 seconds
maximum concurrent operations: 10
```

All values remain configurable.

## Safety boundary

```text
Owner-approved internal Widgets only
no arbitrary shell execution
no arbitrary external-script execution
no third-party marketplace
```

## Status

Batch 4 baseline.