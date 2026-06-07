# KR Desktop Hub CoreHost Widget Developer API v2.0.0

## 1. Purpose

This is the primary public entry point for developers building Widgets for KR Desktop Hub CoreHost `v2.0.0`.

Production Widgets live in a separate repository:

```text
kairosrepublica/kr-desktop-hub-widgets
```

CoreHost provides infrastructure. Widgets provide user-facing business behavior.

## 2. Non-negotiable boundary

A Widget may depend on:

```text
KRDesktopHub.Contracts
KRDesktopHub.WidgetSdk
```

A Widget must not:

```text
import CoreHost implementation assemblies
import another Widget
mutate another Widget's state
own the Windows tray icon
open unmanaged background threads outside governed scheduling
bypass notification governance
resize the outer CoreHost popup
read arbitrary CoreHost package folders
trigger installed-catalog discovery
```

## 3. Package format

Production distribution format:

```text
.krwidget.zip
```

Development-only local folder install may exist behind an advanced CoreHost setting. Production packages use the ZIP path.

Top-level package manifest:

```text
manifest.json
```

Use schema:

```text
KR_Desktop_Hub_Widget_Package_Manifest_Schema_v1.2.json
```

Required package fields include:

```text
manifest_schema_version
widget_id
display_name
package_version
required_contracts_version
minimum_host_version
entry_assembly
entry_type
activation_mode
capabilities
default_enabled
default_collapsed
preferred_expanded_height_dip
minimum_collapsed_height_dip
settings_schema_version
state_schema_version
```

## 4. Compatibility rule

CoreHost `v2.0.0` evaluates:

```text
minimum_host_version
required_contracts_version
package_version
```

A package is rejected before activation if its compatibility metadata is invalid or incompatible.

## 5. Widget identity

Use a stable reverse-domain-style or KR-governed Widget identifier.

Examples:

```text
kr.world-time-space
kr.trading-clock
```

Do not rename a released Widget identifier merely because the display label changes.

## 6. Host layout contract

The CoreHost popup is an Owner-controlled viewport.

```text
default popup width:
600 DIP

minimum popup width:
600 DIP

Widget width:
inherits CoreHost content width

outer popup width and height:
must not be mutated by a Widget
```

A Widget reports its desired internal expanded height through the governed host layout boundary.

The CoreHost decides whether host-level scrolling is required.

`DIP` means device-independent pixel: a logical Windows pixel independent of display scaling.

## 7. Host states

Every Widget supports:

```text
enabled
disabled
expanded
collapsed
```

These are distinct states.

```text
disabled:
not visible in the active host stack
recoverable through Widget Management

collapsed:
still enabled
compact presentation visible
state persists across restart

expanded:
full presentation visible
state persists across restart
```

## 8. Universal Widget Framework

Use the CoreHost-owned UI contract for:

```text
standard Widget chrome
title region
Collapse / Expand control
context-menu entry point
generic state transition
persistent collapsed state
desired-height reporting
host-level scrolling negotiation
floating dialog requests
tray-icon state requests
```

Do not fork private card chrome in each production Widget.

## 9. Capabilities

Capabilities are declarative and deny-by-default.

Relevant capability IDs include:

```text
ui.surface
ui.context-menu
ui.dialog
tray-icon.request
diagnostics.write
state.read
state.write
settings.read
settings.write
```

A manifest must declare requested capabilities. The CoreHost validates them and exposes only governed broker paths.

## 10. Tray icon broker

Widgets never own the Windows tray icon.

A Widget may submit a constrained request using:

```text
approved visual-state key
priority
expiry
fallback behavior
```

The CoreHost:

```text
authorizes the request
arbitrates competing requests
revokes requests from removed or no-longer-approved Widgets
applies the approved icon
falls back safely
```

Arbitrary icon-file paths are not accepted.

## 11. Floating dialogs

Widgets may request desktop-safe floating dialogs through the CoreHost broker.

Widgets must not directly create unmanaged application-level windows that bypass CoreHost placement, ownership and lifecycle policy.

## 12. Scheduling and background work

Widgets must use the governed runtime scheduler.

Do not create permanent background loops.

Use:

```text
activation mode
scheduled cycles
CoreHost cancellation
retry policy
timeout policy
quarantine policy
system-policy suppression
```

## 13. State and settings

Use Widget-specific stores through the governed context:

```text
IWidgetStateStore
IWidgetSettingsStore
```

Do not write directly into another Widget's folder.

Do not write into CoreHost config files.

## 14. Notifications

Use CoreHost notification governance.

Widgets must not bypass:

```text
quiet hours
duplicate suppression
rate limits
priority handling
forced safety-delivery policy
```

## 15. Diagnostics

Use sanitized diagnostics only.

Never log:

```text
passwords
tokens
API keys
credentials
private certificates
raw secrets
private Owner instructions
```

## 16. Installed-catalog refresh boundary

Widgets must not trigger installed-catalog discovery.

Catalog discovery is reserved for:

```text
startup
manual Refresh
successful install
successful uninstall
explicit recovery
```

Ordinary Widget state transitions are discovery-free.

## 17. Example development path

```text
1. Read this document.
2. Read the manifest schema v1.2.
3. Review HelloWidget.
4. Review KRDesktopHub.Fixture.Basic.
5. Create a new isolated Widget project in the Widgets repository.
6. Declare capabilities narrowly.
7. Use the Universal Widget Framework chrome.
8. Add Widget-specific tests.
9. Build a .krwidget.zip package.
10. Install through CoreHost Widget Management.
```

## 18. API references

```text
KR_Desktop_Hub_CoreHost_API_Surface_Map_v2.0.0.md
KR_Desktop_Hub_Widget_API_Batch1.md
KR_Desktop_Hub_Widget_SDK_API_Batch6.md
KR_Desktop_Hub_Universal_Widget_Framework_API_v1.0.md
KR_Desktop_Hub_Widget_Broker_Contracts_API_Batch8D1.md
KR_Desktop_Hub_Widget_Capability_Governance_API_Batch8D1.md
KR_Desktop_Hub_Widget_Package_Installer_API_Batch8C1.md
KR_Desktop_Hub_Widget_Package_Manifest_Schema_v1.2.json
```
