# KR Desktop Hub Settings Center API

## Batch identity

`Stabilization Batch 8D2`

## Purpose

The Settings Center provides a durable, extensible configuration registry and a user-facing interface for CoreHost options.

Each option has:

- a stable key;
- a stable section identifier;
- a description;
- a recommended default;
- a plain-English recommendation reason;
- an application mode.

## Application modes

```text
Immediate
RestartRequired
ReservedForFutureBinding
```

`ReservedForFutureBinding` means the option is intentionally represented in the UI and durable schema, but the corresponding runtime adapter is reserved for a later iteration.

## Sections

```text
startup
panel-tray
hotkeys
notifications
runtime-resources
diagnostics-migration
```

## Durable storage

The Settings Center stores its document under the CoreHost governed data root:

```text
config/corehost-settings-center.json
```

The save workflow writes a temporary file, keeps a local backup of the previous settings document and then performs replacement.

## UI extensibility

The WPF interface is generated from the descriptor catalog. Adding a new settings field requires:

1. adding a property to `CoreHostSettingsCenterState`;
2. adding one descriptor;
3. adding validation when required;
4. adding a runtime binding when the option is not reserved.

The interface does not hard-code an individual XAML control for every setting.