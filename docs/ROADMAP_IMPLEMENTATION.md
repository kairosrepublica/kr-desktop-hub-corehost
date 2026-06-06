# Implementation Roadmap

## Frozen CoreHost baseline

Version:

```text
v0.2.1-rc1
```

Code checkpoint before the public narrative-sync commit:

```text
dabca8f78f26918ebbf0e543175013b46cf244f3
```

The CoreHost architecture is frozen unless production Widget development exposes a real blocking defect.

## Phase 1 Widget sequence

### Checkpoint 1 Γö£├óΓö¼├│Γö£├│╬ô├ç├£Γö¼┬╝Γö£├│╬ô├⌐┬╝Γö¼┬Ñ Public CoreHost narrative synchronization

Scope:

- synchronize README;
- synchronize changelog;
- synchronize public roadmap;
- add public development-record policy;
- tag the frozen public baseline.

### Checkpoint 2 Γö£├óΓö¼├│Γö£├│╬ô├ç├£Γö¼┬╝Γö£├│╬ô├⌐┬╝Γö¼┬Ñ Phase 1 HTML/CSS interaction prototype

Scope:

- KR World Time-Space prototype;
- KR Trading Clock prototype;
- Owner review;
- interaction-specification freeze.

### Checkpoint 3 Γö£├óΓö¼├│Γö£├│╬ô├ç├£Γö¼┬╝Γö£├│╬ô├⌐┬╝Γö¼┬Ñ KR World Time-Space specification

Scope:

- freeze requirements;
- freeze offline holiday-pack boundary;
- freeze user-facing layout;
- define tests.

### Checkpoint 4 Γö£├óΓö¼├│Γö£├│╬ô├ç├£Γö¼┬╝Γö£├│╬ô├⌐┬╝Γö¼┬Ñ KR World Time-Space implementation

Scope:

- implement production Widget;
- package `.krwidget.zip`;
- validate installation;
- run regression tests;
- complete manual acceptance.

### Checkpoint 5 Γö£├óΓö¼├│Γö£├│╬ô├ç├£Γö¼┬╝Γö£├│╬ô├⌐┬╝Γö¼┬Ñ KR Trading Clock specification

Scope:

- freeze exchange-local timelines;
- freeze user-local timelines;
- freeze annual market-calendar boundary;
- define tests.

### Checkpoint 6 Γö£├óΓö¼├│Γö£├│╬ô├ç├£Γö¼┬╝Γö£├│╬ô├⌐┬╝Γö¼┬Ñ KR Trading Clock implementation

Scope:

- implement production Widget;
- package `.krwidget.zip`;
- validate installation;
- run regression tests;
- complete manual acceptance.

### Checkpoint 7 Γö£├óΓö¼├│Γö£├│╬ô├ç├£Γö¼┬╝Γö£├│╬ô├⌐┬╝Γö¼┬Ñ Phase 1 release

Scope:

- publish Phase 1 release notes;
- publish validated packages;
- update public roadmap;
- tag milestone;
- publish GitHub Release when approved.

## Atomic GitHub rule

Every checkpoint above is committed and pushed immediately after validation.

If a checkpoint contains several independently meaningful engineering steps, split it into smaller atomic checkpoints rather than delaying synchronization.

## Final HTML interaction gate v0.7

Validated review targets:

```text
600 DIP popup width
World Time-Space default height: 220 DIP
Trading Clock default height: 500 DIP
World Time-Space row-based height growth
World Time-Space root right-click Add city...
floating Add City chooser above the complete popup
World Time-Space city-card right-click Remove city
Trading Clock byte-identical approved v0.5 layout
Trading Clock collapse-based height shrink in host review shell
Trading Clock expansion-based height restoration
encrypted GitHub Widget-release outer archive
```

Next production track:

```text
KR World Time-Space
```

## Installed-topology path-resolution repair

The Phase 1 review shell now validates installed relative paths before browser acceptance:

```text
World Time-Space:
../../world-time-space-widget/v0.7/index.html

Trading Clock:
../../trading-clock-widget/v0.5/index.html
```

Future HTML review shells and launchers must validate installed topology rather than assuming the source-package topology remains unchanged after installation.


## Universal Widget framework foundation

Before the two Phase 1 production Widgets, implement and validate one generic CoreHost layer:

```text
600 DIP popup default
adaptive measured height
collapse and disable host states
host-level overflow fallback
CoreHost-owned UI tokens
floating-dialog broker
tray-icon request broker
manifest presentation metadata
framework smoke tests
```

This layer is infrastructure only. It does not merge KR World Time-Space and KR Trading Clock.


## CoreHost Checkpoint 2A — installed Widget activation backend

Status:

```text
implemented in this checkpoint
```

Scope:

```text
installed Widget catalog
schema-1 package-to-runtime manifest adapter
installed-folder discovery
adaptive host-state registration
backend enable and disable
backend collapse and expand
backend order updates
runtime-loader support for installed package manifests
regression tests
```

This checkpoint deliberately does not render business Widgets. It freezes the lower-level activation boundary first.

## CoreHost Checkpoint 2B — Windows composition wiring

Status:

```text
next
```

Scope:

```text
Widget Management installed inventory UI
production WidgetHostSurface composition
CoreHost-owned floating-dialog presenter
tray-icon broker application wiring
manual desktop acceptance
```

## Completed checkpoint — 2B Windows Widget composition

The Windows application now owns a shared installed-Widget manager, persistent layout controller, host composition coordinator, floating-dialog presenter and tray-icon application callback. Generic Widget cards are composed from installed catalog entries. Concrete isolated Widget packages attach visual content through the governed Windows visual-surface registry seam.

## CoreHost stabilization checkpoint after 2B manual acceptance

Status:

```text
blocking repair before KR World Time-Space production work
```

Scope:

```text
persist intentional quiet-hours clearing
expose quiet-hours enabled state in Settings Center
reject partial quiet-hours pairs
separate Widget-card collapse from outer-popup viewport shrink
preserve-or-grow outer viewport policy
regression tests
manual acceptance replay
default-state closure validation before runtime overlay
```
