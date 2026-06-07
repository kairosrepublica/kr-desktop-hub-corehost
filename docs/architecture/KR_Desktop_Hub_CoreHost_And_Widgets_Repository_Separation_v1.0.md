# KR Desktop Hub CoreHost and Widgets Repository Separation v1.0

## Purpose

Prevent CoreHost platform code and production Widget code from contaminating each other.

## Canonical repositories

CoreHost platform:

```text
kairosrepublica/kr-desktop-hub-corehost
```

Production Widgets:

```text
kairosrepublica/kr-desktop-hub-widgets
```

## CoreHost repository may contain

```text
Contracts
Widget SDK
sample Widgets
regression fixtures
generic Widget Manager
generic package installer
generic framework
public Widget-facing API docs
```

## CoreHost repository must not contain

```text
KR World Time-Space production code
KR Trading Clock production code
production Widget requirements
production Widget interaction prototypes
market-specific business logic
holiday packs
weather logic
calendar business logic
Widget release artifacts
```

## Widgets repository owns

```text
production Widget roadmap
production Widget requirements
interaction prototypes
production Widget source
Widget-specific tests
Widget-specific release scripts
Widget release artifacts
Widget-specific changelog
Widget-specific GitHub Issues and releases
```

## Allowed dependency direction

```text
Widgets repository
depends on
released CoreHost Contracts and Widget SDK baseline
```

Forbidden:

```text
CoreHost imports production Widget code
one Widget imports another Widget runtime
Widgets mutate CoreHost implementation files
Widgets share mutable runtime state
Widgets own the Windows tray icon
```

## Regression exception

CoreHost may retain:

```text
HelloWidget
KRDesktopHub.Fixture.Basic
```

These are contract examples and regression fixtures, not production business Widgets.

## Migration from historical CoreHost content

Historical Phase 1 production Widget prototypes and requirements move to the separate Widgets repository.

The CoreHost repository deletes those files during the `v2.0.0` shell release-candidate checkpoint.
