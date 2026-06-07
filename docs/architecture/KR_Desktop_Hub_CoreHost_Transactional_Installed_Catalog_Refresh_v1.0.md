# KR Desktop Hub CoreHost — Transactional Installed-Catalog Refresh v1.0

## Status

```text
CoreHost stabilization checkpoint
Production business Widgets remain blocked
```

## 1. Problem closed

Installed-Widget discovery previously mutated active layout registration, measured-height state and persisted host state before the candidate catalog passed degraded-snapshot acceptance.

The host could retain the prior visible panel while internal state had already partially moved forward.

## 2. Transactional refresh boundary

```text
explicit refresh trigger
then
pure staged discovery
then
candidate acceptance
then
exact accepted-catalog commit
then
exact capability reconciliation
then
stale tray-icon request revocation
then
panel reconciliation
```

Staged discovery does not:

```text
mutate active registrations
persist host state
emit LayoutChanged
reconcile capabilities
mutate tray requests
replace panel composition
```

## 3. Accepted-catalog reconciliation

Accepted commit performs:

```text
exact active-registration replacement
stale measured-height pruning
default seeding for newly accepted Widgets
single persistence write
single LayoutChanged emission
```

Dormant Owner preferences remain intentionally preserved so reinstall can restore enabled, collapsed and order state unless the Owner later chooses an explicit purge workflow.

## 4. Degraded-candidate protection

When discovery contains failures, any disappearance from the previously accepted installed catalog rejects the complete candidate, including disappearance of a previously disabled Widget.

## 5. Capability and tray-icon boundary

Accepted catalog commit triggers exact capability approval reconciliation. Removed or reduced capability sets are pruned.

The CoreHost tray broker then revokes active requests submitted by Widgets no longer approved for `tray-icon.request`, recalculates the winning request and applies the fallback when required.

## 6. LayoutChanged boundary

The Windows host subscribes to framework `LayoutChanged` events and updates host-level scrolling only.

Layout changes do not:

```text
discover packages
replace accepted catalog
resize outer popup width
resize outer popup height
```

## 7. Widget Manager boundary

Widget Manager manual refresh and post-install refresh use one accepted-catalog coordinator delegate. State-only controls remain on the in-memory projection path.

## 8. Remaining shell gate

```text
ShowActivated policy
ShowInTaskbar policy
unconditional Activate removal
Microsoft Pinyin indicator replay
popup lower shadow replay
taskbar notification-area darkening replay
sanitized shell diagnostics
Owner manual acceptance
```
