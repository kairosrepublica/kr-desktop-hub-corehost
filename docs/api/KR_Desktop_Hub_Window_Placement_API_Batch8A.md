# KR Desktop Hub Window Placement API â€” Stabilization Batch 8A

## Purpose

Persist and restore the main panel's last usable size and position across explicit application exit and relaunch.

## Data root

The default data root resolves from the current Windows Documents folder:

```text
<KnownFolder: Documents>\KRG\KRG Dock\KRG App\KRDesktopHub\
```

Advanced override:

```text
KRDESKTOPHUB_DATA_ROOT
```

## State file

```text
<DATA_ROOT>\state\window-placement.json
```

## Types

```text
CoreHostDataRootResolver
WindowPlacementState
WindowPlacementDefaults
MonitorWorkingArea
JsonWindowPlacementStore
WindowPlacementPolicy
WindowsWindowPlacementService
```

## Save behavior

```text
debounced save after location changes
debounced save after size changes
save when panel is hidden
save during explicit application exit
save RestoreBounds when window is minimized or maximized
```

## Restore behavior

```text
restore before first display
preserve maximized state
never restore minimized state
clamp invalid geometry
recover when a previous monitor is missing
use monitor working areas rather than raw screen bounds
```

## Boundary

This batch implements durable window-placement state only.

CoreHost-wide user settings and Widget package installation remain separate stabilization batches.