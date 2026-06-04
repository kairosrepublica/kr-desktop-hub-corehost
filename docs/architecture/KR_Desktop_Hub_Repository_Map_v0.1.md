# KR Desktop Hub Repository Map v0.1

## Root

```text
README.md
CHANGELOG.md
ROADMAP.md
SECURITY.md
.gitignore
.editorconfig
LICENSE_DECISION_PENDING.md
KR_Desktop_Hub.sln
Directory.Build.props
START_HERE.ps1
```

## Public source tree

```text
src/
├─ KRDesktopHub.Contracts/
├─ KRDesktopHub.Platform.Abstractions/
├─ KRDesktopHub.Platform.Windows/
├─ KRDesktopHub.Core/
├─ KRDesktopHub.WidgetSdk/
└─ KRDesktopHub.App.Windows/
```

These directories are placeholders during Batch 0.

Actual `.NET` projects are initialized during Batch 1.

## Widgets

```text
widgets/
├─ fixtures/
└─ production/
```

Production Widgets are intentionally deferred.

## Tests

```text
tests/
├─ KRDesktopHub.Contracts.Tests/
├─ KRDesktopHub.Core.Tests/
├─ KRDesktopHub.Platform.Windows.Tests/
├─ KRDesktopHub.WidgetRuntime.Tests/
├─ KRDesktopHub.Integration.Tests/
└─ KRDesktopHub.CleanRelease.Tests/
```

## Tools

```text
tools/
├─ advanced/
├─ fixtures/
└─ templates/
```

Only `START_HERE.ps1` is a normal operator entry point.

## Local-only excluded tree

```text
owner_private_docs/
```

This folder is intentionally excluded by `.gitignore`.
