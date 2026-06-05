# KR Trading Clock Widget Prototype v0.5

Standalone HTML/CSS/JavaScript prototype for Widget 02.

## v0.5 layout changes

Removed:

```text
Icon B · U.S. regular session closed
Exchange-local 00:00–24:00
Europe/Istanbul mapped to the same exchange-day positions
Data-driven market registry reserved for future exchanges.
```

Changed:

```text
Market title rows now show Open or Closed.
Open uses bold green text.
Closed uses grey text.
```

Added:

```text
The orange current-time marker on each exchange-local timeline displays the current exchange-local time.
The orange current-time marker on each Local timeline displays the corresponding current local time.
```

## Preserved rules

```text
600 DIP normal-width baseline
independent market-card collapse
exchange-local top timeline
mapped local bottom timeline
single-line compact session chips
data-driven market registry retained internally for future extensibility
```
