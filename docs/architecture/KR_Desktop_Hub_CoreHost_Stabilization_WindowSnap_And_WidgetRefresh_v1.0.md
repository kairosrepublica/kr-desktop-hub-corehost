# KR Desktop Hub CoreHost Stabilization — Window Snap and Widget Refresh Integrity v1.0

## Purpose

This checkpoint repairs two Owner-observed host-boundary defects before KR World Time-Space production implementation begins.

## Defect A — vertically snapped shell released after Widget collapse

Observed behavior:

```text
move popup near a screen edge
Windows expands it from top to bottom
collapse one Widget
outer popup immediately becomes shorter
```

Root cause:

```text
current shell height was bounded by the automatic-growth work-area cap
Height was reassigned after every refresh even when no growth was required
```

Correct policy:

```text
preserve user- or Windows-expanded observed shell height
automatic-growth cap constrains growth only
never shrink shell merely because a Widget collapsed
avoid assigning Height when no growth is required
use ActualHeight as the observed snapped geometry
```

## Defect B — transient empty host after collapse or expand

Observed behavior:

```text
click Expand or Collapse
visible Widget sometimes disappears
empty state appears
click Refresh
Widget reappears
```

Risk boundary:

```text
overlapping host mutations and catalog refreshes
non-transactional panel replacement
catalog refresh allowed a degraded temporary snapshot to replace the last known-good panel
```

Correct policy:

```text
serialize installed-catalog refreshes
serialize host mutations
reject a degraded snapshot when it would remove a previously visible Widget
preserve last known-good panel
build the next card set before replacing the visible card set
publish a governed notification when an observed UI operation fails
```

## Regression gates

```text
vertically snapped viewport remains unchanged after collapse
collapse that requires no growth does not assign outer Height
automatic growth remains capped
serial queue rejects overlap
serial queue preserves ordering
degraded catalog snapshot cannot replace last known-good visible host
explicit disable snapshot remains accepted
existing 16 smoke-test projects
clean-extraction win-x64 EXE self-test
Owner manual acceptance replay
```

## Superseding geometry clarification

The earlier `preserve user- or Windows-expanded observed shell height` rule is superseded by a stricter Owner-sized viewport rule:

```text
CoreHost Widget operations never assign outer Width.
CoreHost Widget operations never assign outer Height.
The Owner controls the outer shell geometry.
Content overflow is handled by host-level scrolling.
```
