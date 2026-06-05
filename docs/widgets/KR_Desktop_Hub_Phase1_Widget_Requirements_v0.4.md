# KR Desktop Hub Phase 1 Widget Requirements v0.4

## Phase 1 Widgets

```text
KR World Time-Space
KR Trading Clock
```

They remain fully separate production Widgets.

## Common width

```text
600 DIP
```

## World Time-Space

```text
default height:
220 DIP

automatic row-based height growth:
48 DIP per row beyond two rows

default city-card capacity:
8 visible slots

maximum cities:
21
```

City cards show:

```text
city
time
weekday and date
time-zone abbreviation
HOL or â€” statutory-holiday badge
```

City cards do not show:

```text
country
region
remove cross icon
```

Interactions:

```text
right-click Widget:
Add city...

right-click removable city:
Add city...
Remove city
```

`Add city...` opens an unclipped floating chooser above the complete popup or Windows desktop layer.

## Trading Clock

```text
approved visual baseline:
v0.5

default height:
500 DIP

collapse:
auto-shrink height

expand:
auto-grow height

future markets:
auto-grow height
```

## Release distribution rule

The internal installer package remains:

```text
.krwidget.zip
```

The GitHub downloadable release artifact must be an outer encrypted archive:

```text
.7z
AES-256 encryption
```

The outer archive contains:

```text
validated .krwidget.zip
SHA-256 file
authorization notice
```

The archive password must never be committed, published in release notes, written into public scripts or exposed in logs.

Public release text must instruct users:

```text
To request free authorization and the extraction password,
email kr@kairosrepublica.com.
```
