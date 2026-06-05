# KR World Time-Space Widget Prototype v0.7

## Purpose

Final HTML/CSS/JavaScript interaction gate before production Widget implementation.

## Default height

```text
220 DIP
```

The default height accommodates two complete rows and eight city-card slots.

## Dynamic height

```text
visibleRows = ceil(visibleCityCount / 4)

desiredHeight =
    220 DIP
    + max(0, visibleRows - 2) * 48 DIP
```

The city grid does not use an internal vertical scrollbar.

## Context menus

Right-click inside the Widget:

```text
Add city...
```

Right-click a removable city card:

```text
Add city...
Remove city
```

`Local` is protected.

Selecting `Add city...` opens a floating chooser above the full review shell when embedded, or a standalone full-surface overlay when this prototype is opened directly.

## Holiday badge

```text
HOL
local statutory holiday

—
not a local statutory holiday
```

The prototype holiday pack is intentionally limited. Production uses versioned annual offline packs with explicit provenance.
