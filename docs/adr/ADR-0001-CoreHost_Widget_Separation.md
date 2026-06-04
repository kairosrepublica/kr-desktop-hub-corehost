# ADR-0001: Separate CoreHost from Widgets

## Status

Accepted.

## Decision

CoreHost provides infrastructure only.

User-facing capabilities are implemented as independent Widgets.

## Reason

This keeps the center stable and allows future functionality to grow without destabilizing the host.

## Consequences

CoreHost must never depend on a concrete Widget.
