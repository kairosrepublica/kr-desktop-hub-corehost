# KR Desktop Hub Widget Runtime Execution Policy API

## Batch identity

`Stabilization Batch 8B2B`

## Purpose

This layer connects the existing CoreHost system-policy evaluator to Widget Runtime execution.

It does not implement any production Widget.

## Main types

### `SystemPolicyWidgetExecutionGate`

Stores the workload profile for each internal Widget and converts the current CoreHost system-policy decision into an operation-level allow-or-suppress decision.

### `PolicyEnforcedWidgetRuntimeController`

Wraps the baseline `WidgetRuntimeController`.

It prevents `StartAsync()` and `ResumeAsync()` from reaching a Widget when current system policy suppresses execution.

A policy suppression is raised before the baseline lifecycle controller runs. It is not counted as a Widget failure and does not advance the quarantine counter.

### `PolicyAwarePeriodicWidgetScheduler`

Implements `IWidgetScheduler`.

Before each scheduled cycle it:

1. evaluates current CoreHost policy;
2. applies the current refresh-interval multiplier;
3. skips blocked cycles without invoking the Widget callback;
4. records executed and suppressed cycle counts for diagnostics.

## Supported work kinds

- `General`
- `NetworkRequest`
- `VisualRefresh`

## Current policy effects

The current CoreHost settings can therefore govern:

- low-priority Widget execution while using battery power;
- network-heavy Widget requests while Windows is locked;
- Widget network requests while offline;
- inactive Widget network requests;
- visual refresh while the panel is hidden;
- battery-aware and panel-visibility-aware refresh intervals.

## Boundary

This batch establishes the CoreHost runtime enforcement interface.

Future internal Widgets must receive a policy-aware scheduler through their Widget context. Production Widget installation and package management remain separate stabilization work.