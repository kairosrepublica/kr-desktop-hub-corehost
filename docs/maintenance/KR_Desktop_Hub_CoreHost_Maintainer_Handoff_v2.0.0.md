# KR Desktop Hub CoreHost Maintainer Handoff v2.0.0

## Purpose

Give future maintainers one public map of the stable CoreHost platform.

## Canonical repository

```text
kairosrepublica/kr-desktop-hub-corehost
```

## Product boundary

CoreHost is infrastructure.

Production business Widgets belong in:

```text
kairosrepublica/kr-desktop-hub-widgets
```

## Read order

```text
README.md
docs/Product_Scope.md
docs/Architecture.md
docs/ROADMAP_IMPLEMENTATION.md
docs/api/KR_Desktop_Hub_API_Index.md
docs/api/KR_Desktop_Hub_CoreHost_Widget_Developer_API_v2.0.0.md
docs/api/KR_Desktop_Hub_CoreHost_API_Surface_Map_v2.0.0.md
docs/architecture/KR_Desktop_Hub_CoreHost_StateOnly_WidgetHost_Transitions_v1.0.md
docs/architecture/KR_Desktop_Hub_CoreHost_Transactional_Installed_Catalog_Refresh_v1.0.md
docs/architecture/KR_Desktop_Hub_CoreHost_Windows_Shell_Stabilization_v1.0.md
docs/architecture/KR_Desktop_Hub_CoreHost_And_Widgets_Repository_Separation_v1.0.md
```

## Stable invariants

```text
ordinary Widget-host state changes are discovery-free
catalog refresh is stage, accept, then commit
rejected degraded candidates mutate nothing
outer popup geometry is Owner-controlled
host-level scrolling handles overflow
CoreHost owns tray icon arbitration
Widgets request approved tray states only
production Widgets remain isolated
ordinary popup Show does not force activation
```

## Regression fixtures

```text
samples/HelloWidget
widgets/fixtures/KRDesktopHub.Fixture.Basic
tests/*SmokeTests
```

## Release workflow

```text
run complete Release build
build every discovered smoke-test project
run every discovered smoke test
publish self-contained win-x64
clean-extraction self-test
run Owner manual shell acceptance
tag and publish GitHub release
```

## Private governance

Private coding rules, error-experience case studies and Owner instructions remain outside public GitHub.
