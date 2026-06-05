# KR Desktop Hub Widget Panel Layout Baseline v0.4

## Default width

```text
600 device-independent pixels (DIP)
```

## KR World Time-Space height

```text
Default height:
220 DIP

Default visible capacity:
2 rows
4 city-card slots per row
8 visible city-card slots
```

Dynamic growth:

```text
visibleRows = ceil(visibleCityCount / 4)

desiredHeight =
    220 DIP
    + max(0, visibleRows - 2) * 48 DIP
```

Do not use an internal vertical scrollbar for ordinary city growth.

## KR Trading Clock height

```text
Default height:
500 DIP
```

Dynamic behavior:

```text
collapse market:
reduce height automatically

expand market:
increase height automatically

add future market:
increase height automatically
```

Trading Clock layout remains the approved `v0.5`.

## World Time-Space context menus

Root right-click menu:

```text
Add city...
```

Removable city-card right-click menu:

```text
Add city...
Remove city
```

`Local` cannot be removed.

Selecting `Add city...` opens a floating chooser above the overall popup or Windows desktop layer. The chooser must not be clipped by the World Time-Space Widget bounds.
