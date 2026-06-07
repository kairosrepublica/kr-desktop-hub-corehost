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
Universal Widget Framework
+
Widget SDK
+
Independent Widgets repository
+
Controlled configuration
+
Diagnostics and rollback
```

## CoreHost layers

```text
KRDesktopHub.Contracts
KRDesktopHub.Platform.Abstractions
KRDesktopHub.Platform.Windows
KRDesktopHub.Core
KRDesktopHub.WidgetSdk
KRDesktopHub.App.Windows
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

Independent Widgets
    depend on Contracts
    may depend on WidgetSdk

Core
    must never depend on a concrete production Widget
```

## Non-negotiable boundaries

```text
No Windows Presentation Foundation type inside Contracts.
No Windows-specific API inside platform-neutral Contracts.
No market logic inside CoreHost.
No calendar logic inside CoreHost.
No reminder logic inside CoreHost.
No production Widget implementation inside the CoreHost repository.
No Widget-controlled permanent background threads.
No direct Widget bypass of centralized scheduling.
No direct Widget bypass of centralized notifications.
No direct Widget ownership of the Windows tray icon.
No ordinary state-only mutation coupled to filesystem discovery.
No candidate catalog mutation before acceptance.
```

## Widget-host state model

```text
state-only operation:
mutate in-memory accepted projection
persist generic host state
reconcile affected host card
do not discover packages

explicit catalog refresh:
stage discovery
validate candidate
evaluate acceptance
commit exact reconciliation once
```

## Windows shell model

```text
popup show:
ShowActivated = false
ShowInTaskbar = false
ordinary Show does not call Activate()

popup hide:
centralized HidePanel path
system-policy visibility synchronized

diagnostics:
sanitized shell.panel.lifecycle JSONL records
exported through governed diagnostics tooling
```

## Repository separation

CoreHost public repository:

```text
kairosrepublica/kr-desktop-hub-corehost
```

Production Widgets public repository:

```text
kairosrepublica/kr-desktop-hub-widgets
```

Only Contracts, SDK examples, fixtures and API documentation may cross the boundary.
