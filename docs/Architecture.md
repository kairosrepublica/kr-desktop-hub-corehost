# Architecture

## Architectural model

```text
Stable CoreHost
+
Versioned Contracts
+
Platform abstractions
+
Windows adapter
+
Widget SDK
+
Independent Widgets
+
Controlled configuration
+
Diagnostics and rollback
```

## Planned layers

```text
KRDesktopHub.Contracts
KRDesktopHub.Platform.Abstractions
KRDesktopHub.Platform.Windows
KRDesktopHub.Core
KRDesktopHub.WidgetSdk
KRDesktopHub.App.Windows
Widgets
Tests
Tools
Docs
```

## Dependency direction

```text
Contracts
    depends on nothing project-specific

Platform.Abstractions
    depends on Contracts

Platform.Windows
    depends on Platform.Abstractions
    depends on Contracts

Core
    depends on Contracts
    depends on Platform.Abstractions

WidgetSdk
    depends on Contracts

App.Windows
    depends on Core
    depends on Platform.Windows

Widgets
    depend on Contracts
    may depend on WidgetSdk

Core
    must never depend on a concrete Widget
```

## Non-negotiable boundaries

```text
No WPF type inside Contracts.
No Windows-specific API inside platform-neutral Contracts.
No market logic inside CoreHost.
No calendar logic inside CoreHost.
No reminder logic inside CoreHost.
No Widget-controlled permanent background threads.
No direct Widget bypass of centralized scheduling.
No direct Widget bypass of centralized notifications.
```

## Current status

Batch 0 creates the source-control and documentation skeleton only.

The `.NET` projects are intentionally initialized during Batch 1 after the local Windows development environment is verified.
