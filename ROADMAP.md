# Roadmap

## CoreHost v2.0.0

Status:

```text
release candidate pending Owner Windows shell manual acceptance
```

Completed platform capabilities:

```text
Windows tray lifecycle
non-disruptive panel show / hide policy
window-placement persistence
durable CoreHost settings
Settings Center
notification governance
system-policy handling
resource governance
diagnostics export
migration import and export
Widget contracts
Widget SDK
Widget manifest validation
Widget package installer
Widget Manager
Universal Widget Framework
state-only discovery-free transitions
transactional installed-catalog refresh
exact active-registration reconciliation
capability pruning
tray-request revocation
host-level scrolling
portable release self-test
```

## CoreHost v2.0.0 final release gate

Remaining actions:

```text
Owner manual shell replay
confirm no Microsoft Pinyin Chinese / English indicator jump
confirm no tray notification-area darkening
confirm popup remains manually interactive
confirm close-to-tray behavior
confirm Owner geometry persistence
generate portable v2.0.0 ZIP
tag v2.0.0
publish GitHub release
```

## Production Widgets repository

Production Widgets are intentionally separate:

```text
kairosrepublica/kr-desktop-hub-widgets
```

Planned sequence:

```text
Widget 01:
KR World Time-Space

Widget 02:
KR Trading Clock
```

CoreHost fixtures remain in this repository only to prove the platform contract.

## Future CoreHost work after v2.0.0

```text
Windows ARM64 evaluation
modern Windows Notification Center transport
explicit diagnostics-export UI
optional installer
automatic updater evaluation
future Contracts versioning
```

## Public checkpoint discipline

Every validated Owner-approved engineering step receives an immediate scoped commit and immediate push to `origin/main`.

Narrative evidence is synchronized in the same checkpoint whenever public project state changes.
