# KR Desktop Hub Widget SDK API â€” Batch 6 Baseline

## Purpose

Batch 6 adds the first helper library for internal Widget development.

## SDK types

```text
KrWidgetBase
WidgetManifestDocument
WidgetManifestFile
```

## Lifecycle helper

Extend:

```csharp
KrWidgetBase
```

Override only the lifecycle hooks needed by the Widget:

```text
OnInitializeAsync
OnStartAsync
OnPauseAsync
OnResumeAsync
OnStopAsync
```

## State and settings helpers

```text
GetSettingAsync
ReadStateAsync
WriteStateAsync
```

## Manifest helper

```csharp
var manifest =
    WidgetManifestFile.Create(
        widget,
        entryAssembly,
        entryType,
        activationMode);

await WidgetManifestFile.WriteAsync(
    path,
    manifest,
    cancellationToken);
```

## Boundary

The SDK is for Owner-approved internal Widgets.

The SDK does not expose arbitrary shell execution or arbitrary external-script execution.

## Example

```text
samples/HelloWidget
```

## Status

Batch 6 baseline.