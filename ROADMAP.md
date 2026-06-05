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

## Phase 1 â€” Production Widget foundation

### Step 1 â€” HTML/CSS interaction prototype gate

Status: in progress.

Prepare and approve an interactive browser prototype before writing Windows Widget production code.

### Step 2 â€” KR World Time-Space Widget

Status: planned.

Build a synchronized multi-city time-comparison Widget with date, local time, time-zone and holiday context.

### Step 3 â€” KR Trading Clock Widget

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
