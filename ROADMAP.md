# Roadmap

## CoreHost baseline

Status: frozen at `v0.2.1-rc1`.

Public code checkpoint before the narrative-sync commit:

```text
dabca8f78f26918ebbf0e543175013b46cf244f3
```

Completed CoreHost capabilities include:

- Windows tray lifecycle;
- panel show-hide lifecycle;
- window-placement persistence;
- durable CoreHost settings;
- Settings Center;
- notification governance;
- system-policy handling;
- resource governance;
- diagnostics export;
- migration import and export;
- Widget manifest validation;
- Widget package installer;
- Widget Manager;
- broker contracts;
- Widget SDK sample;
- portable self-test mode.

The popup panel is intentionally blank and reserved for production Widgets.

## Phase 1 Γö£├│╬ô├⌐┬╝╬ô├ç┬Ñ Production Widget foundation

### Step 1 Γö£├│╬ô├⌐┬╝╬ô├ç┬Ñ HTML/CSS interaction prototype gate

Status: in progress.

Prepare and approve an interactive browser prototype before writing Windows Widget production code.

### Step 2 Γö£├│╬ô├⌐┬╝╬ô├ç┬Ñ KR World Time-Space Widget

Status: planned.

Build a synchronized multi-city time-comparison Widget with date, local time, time-zone and holiday context.

### Step 3 Γö£├│╬ô├⌐┬╝╬ô├ç┬Ñ KR Trading Clock Widget

Status: planned.

Build United States and Hong Kong market-clock comparisons with exchange-local and user-local 24-hour timelines.

## Public checkpoint discipline

Every validated Owner-approved engineering step receives an immediate scoped commit and immediate push to `origin/main`.

Narrative evidence is synchronized at the same checkpoint when the public product state changes.

## Phase 1 final HTML-gate approval

Status: final HTML/CSS/JavaScript interaction gate prepared for Owner browser review.

```text
World Time-Space:
v0.7

Trading Clock:
approved v0.5, preserved byte-for-byte

Review shell:
v0.7
```

After Owner approval, production engineering begins with KR World Time-Space as a standalone Widget.


## Universal Widget framework foundation

Status: implementation checkpoint prepared.

Before production Widgets, CoreHost gains one reusable framework:

```text
600 DIP default popup width
adaptive Widget height negotiation
collapsed, expanded and disabled host states
shared CoreHost UI token layer
floating-dialog request broker
tray-icon request broker
approved visual-state registry
```

Production sequence after framework validation:

```text
KR World Time-Space
KR Trading Clock
```


## CoreHost Widget Management production wiring

Status: backend activation boundary implemented; Windows UI wiring remains next.

Completed backend checkpoint:

```text
installed Widget catalog
snake_case package manifest to camelCase runtime manifest adapter
installed-folder discovery
host-state registration
backend enable and disable
backend collapse and expand
backend ordering
runtime-loader support for installed package manifests
```

Next CoreHost checkpoint:

```text
Widget Management installed inventory UI
production WidgetHostSurface composition
CoreHost-owned floating-dialog presenter
tray-icon broker application wiring
```

## Completed — Checkpoint 2B Windows Widget composition

- Widget Management installed inventory UI
- enable, disable, expand, collapse and ordering controls
- production generic WidgetHostSurface composition
- 600-DIP adaptive popup refresh
- CoreHost-owned floating-dialog presenter
- CoreHost-owned tray-icon broker application wiring
- non-breaking integrated Widget context seam

## CoreHost stabilization before production Widgets

Status: required before KR World Time-Space production implementation.

Owner manual acceptance exposed two blocking defects:

```text
quiet-hours clear intent was not persistent
Widget-card collapse incorrectly shrank the outer CoreHost popup
```

The stabilization checkpoint fixes both behaviors and adds regression coverage before Widget 01 begins.

The repair also closes a default-state contradiction: the Settings Center quiet-hours default now uses a valid 23:00–08:00 pair before runtime overlay.


## CoreHost stabilization follow-up — snapped geometry and refresh integrity

Status: required Owner-acceptance repair before production Widget 01.

```text
preserve vertically snapped outer popup geometry during Widget collapse
apply work-area cap only to automatic growth
serialize host mutations
serialize installed-catalog refreshes
retain last known-good panel during degraded catalog reads
replace visible Widget cards transactionally
```

## CoreHost stabilization gate — Owner-sized viewport

Before Widget 01 begins, the CoreHost shell must treat popup geometry as Owner-controlled state.

```text
minimum width:
600 DIP

expand / collapse:
child Widget only

overflow:
host-level scrolling

outer shell:
no Widget-driven width or height mutation
```
