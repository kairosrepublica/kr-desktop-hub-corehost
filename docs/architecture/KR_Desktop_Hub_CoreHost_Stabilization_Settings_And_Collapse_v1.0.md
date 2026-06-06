# KR Desktop Hub CoreHost Stabilization — Settings Clearing and Widget Collapse Isolation v1.0

## Purpose

Owner manual acceptance after Checkpoint 2B exposed two blocking defects before production Widget development.

## Defect A — quiet-hours clear intent was lost

Settings Center described quiet-hours start and end as optional fields, but the UI-to-runtime bridge converted blank values back to the previous runtime values.

The stabilized behavior is:

```text
clear both fields
save
quiet hours become disabled
reload
close and reopen Settings Center
both fields remain blank
```

A partial pair is rejected:

```text
start blank and end populated
or
start populated and end blank
```

Explicit re-enable requires:

```text
QuietHoursEnabled = true
valid start in HH:mm
valid end in HH:mm
```

The runtime may retain valid internal fallback times while quiet hours are disabled. The UI intentionally presents blank fields when the feature is disabled.

## Default-state closure

The Settings Center document must be valid before the Windows runtime bridge overlays runtime-backed values.

The stabilized recommended editor state is:

```text
QuietHoursEnabled:
true

QuietHoursStart:
23:00

QuietHoursEnd:
08:00
```

This avoids a self-contradictory first-run path:

```text
create default document
save default document
validate default document
```

Clearing both fields later remains a distinct Owner intent and disables quiet hours.

## Defect B — Widget-card collapse resized the outer popup

The internal layout model must reduce the collapsed Widget card to its collapsed height.

However, the outer CoreHost popup must not automatically shrink merely because one Widget is collapsed.

The stabilized policy is:

```text
Widget card:
collapse and expand normally

internal desired layout height:
recompute normally

outer CoreHost popup:
preserve current height
grow when more space is required and available
never shrink automatically because a Widget card collapsed
use host-level scrolling when desired content exceeds the work-area cap
```

## Regression gates

```text
quiet-hours recommended-default closure before runtime overlay
quiet-hours clear, save, reload and reopen
quiet-hours explicit re-enable
partial-pair rejection
enabled-but-blank rejection
Widget internal collapse height
outer viewport collapse isolation
host-level overflow fallback
complete solution build
all smoke tests
clean-extraction executable self-test
Owner manual acceptance replay
```
