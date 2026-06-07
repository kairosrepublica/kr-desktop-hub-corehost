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

## Default CoreHost icon

The default branded KR icon is owned by CoreHost:

```text
src/KRDesktopHub.App.Windows/Assets/CoreHost/KRDesktopHub.CoreHost.ico
src/KRDesktopHub.App.Windows/Assets/CoreHost/KRDesktopHub.CoreHost.png
src/KRDesktopHub.App.Windows/Assets/CoreHost/KRDesktopHub.CoreHost.svg
```

Usage:

```text
application executable icon
WPF popup-window icon
tray default visual state
```

Widgets must not submit arbitrary icon-file paths or directly own the tray icon.

## Portable-release helper invariant

```text
tools/BUILD_VERIFY_PORTABLE_RELEASE.ps1
```

must:

```text
discover exactly one root solution
anchor both PowerShell and process working directories
discover every smoke-test project dynamically
convert absolute project paths with parser-safe method invocation
expand untracked paths during repository-clean checks
```

## Final tray-popup shell invariant

```text
MainWindow tray popup:
ShowActivated = false
ShowInTaskbar = false
WS_EX_NOACTIVATE = enabled
ordinary Show does not call Activate()
title-bar minimize is intercepted and converted to HidePanel
title-bar close hides to tray by default
standard Collapse / Expand control is non-focusable
```

Do not apply `WS_EX_NOACTIVATE` to Settings Center, Widget Management or governed dialogs.

Widgets requiring text entry or input-method-editor composition must use governed dialogs.
