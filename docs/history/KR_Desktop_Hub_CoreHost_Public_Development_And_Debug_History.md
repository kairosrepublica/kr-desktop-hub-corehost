# KR Desktop Hub CoreHost — Public Development and Debug History

## Purpose

This document is the sanitized public engineering record for the CoreHost platform.

It records meaningful product iterations, release checkpoints and debug corrections without exposing private Owner instructions, local machine paths, private logs, credentials or internal failure transcripts.

## Development principles

```text
truthful public history
atomic engineering checkpoints
immediate push
timely GitHub Release publication
README summary synchronization
CHANGELOG detail synchronization
private-debug and public-history separation
CoreHost and production-Widgets repository separation
```

## Chronological engineering history

### Bootstrap and foundation

| Commit | Public event | Engineering result |
|---|---|---|
| `17b04c7` | Architecture baseline | Established CoreHost scope and architecture baseline |
| `b28768b` | Automated checkpoints | Added baseline completion and automated checkpoint tooling |
| `cd33e12` | Contracts | Added CoreHost contracts and platform abstractions |
| `1f58673` | Runtime foundation | Added CoreHost runtime foundation |
| `ff51e6a` | Windows integration | Added tray shell and desktop integration |
| `791d5a0` | Runtime loading | Added Widget runtime loader, scheduler and quarantine |
| `e206cef` | Resource governance | Added system policies and resource governance |
| `a5bd769` | Diagnostics and SDK | Added diagnostics migration and Widget SDK |
| `dfd6c0f` | Portable RC | Added portable release-candidate build and validation |
| `0ac7ba1` | Packaging hygiene | Ignored local portable release sidecar artifacts |
| `51156f9` | Placement | Persisted and restored CoreHost window placement |
| `db3fd0d` | Settings | Added durable CoreHost settings and hotkey-conflict handling |

### Governance and Widget-platform expansion

| Commit | Public event | Engineering result |
|---|---|---|
| `07bef9a` | Policy binding | Bound system policies and notification governance to CoreHost settings |
| `723e91a` | Tooltip correction | Replaced corrupted tray tooltip with stable ASCII status text |
| `1183cf2` | Runtime enforcement | Enforced system policies in Widget Runtime execution |
| `28fe592` | Package installer | Added validated internal Widget package-installer foundation |
| `09f9a1f` | Widget Manager | Added explicit Owner-controlled Widget Manager workflow |
| `534bc97` | Capability governance | Added Widget capability governance and broker contracts |
| `fda2b2b` | Settings Center | Added durable Settings Center and UI-governance registry |
| `4a7485c` | Composition root | Moved Settings Center runtime bridge into Windows composition root |
| `f71edeb` | Documentation freeze | Froze public `0.2.x` release documentation |
| `dabca8f` | Blank host surface | Reserved main panel surface for future Widgets |
| `0aed4ba` | Public checkpoint policy | Synchronized CoreHost freeze narrative and checkpoint policy |

### Widget framework and host composition

| Commit | Public event | Engineering result |
|---|---|---|
| `9691426` | HTML interaction gate | Froze Phase 1 review-shell interaction gate |
| `83dad12` | Review-shell correction | Corrected iframe paths |
| `529d342` | Universal Widget Framework | Added shared Widget-framework foundation |
| `0793298` | Installed catalog | Added installed Widget catalog and runtime manifest adapter |
| `f570e83` | Host composition | Wired Widget Management UI and production host composition |

### Stabilization

| Commit | Public event | Engineering result |
|---|---|---|
| `2537449` | Settings and viewport repair | Stabilized settings clearing and Widget-collapse viewport behavior |
| `ba2b9fb` | Owner-sized viewport | Locked owner-sized shell viewport and stabilized Widget-refresh integrity |
| `084e97e` | State-only transition repair | Decoupled Widget-host state transitions from catalog discovery |
| `35b3080` | Transactional catalog | Made installed-Widget catalog refresh transactional |
| `38d5dde` | Shell RC | Stabilized Windows shell and prepared CoreHost `v2.0.0` |
| `7418406` | Branding | Set default KR CoreHost icon |
| `71a10a5` | Portable-tool hardening | Hardened portable release smoke-test discovery |

## Sanitized debug history

### Tray tooltip corruption

```text
Symptom:
tray tooltip displayed corrupted text

Correction:
replace unsafe tooltip output with stable ASCII status text

Public checkpoint:
723e91a
```

### Settings clearing

```text
Symptom:
quiet-hours state could not be intentionally cleared and reloaded cleanly

Correction:
close clearing semantics across save, reload and Settings Center validation

Public checkpoint:
2537449
```

### Widget collapse versus outer popup geometry

```text
Symptom:
collapsing a Widget affected the outer popup viewport

Correction:
treat Widget card height and outer popup geometry as separate responsibilities
preserve Owner-sized viewport
use host-level scrolling for overflow

Public checkpoints:
2537449
ba2b9fb
```

### Collapse / Expand versus package discovery

```text
Symptom:
ordinary Collapse / Expand could reach installed-catalog refresh and degraded-snapshot rejection

Correction:
make ordinary state transitions discovery-free
project accepted catalog state in memory
reconcile affected surfaces without recreating unrelated cards

Public checkpoint:
084e97e
```

### Candidate discovery versus accepted host state

```text
Symptom:
catalog refresh risked mutating host state before candidate acceptance

Correction:
pure staged discovery
candidate acceptance gate
exact active-registration reconciliation
stale measured-height pruning
capability reconciliation
tray-request revocation

Public checkpoint:
35b3080
```

### Shell activation and input-method-editor disturbance

```text
Symptom:
popup lifecycle disturbed taskbar-edge visuals or input-method-editor behavior

Correction:
ShowActivated = false
ShowInTaskbar = false
ordinary Show does not force Activate()
WS_EX_NOACTIVATE for the main tray popup only
title-bar minimize routes to tray hiding
sanitized lifecycle diagnostics

Public checkpoint:
38d5dde
```

### CoreHost branding

```text
Symptom:
default Windows icon did not represent KR Desktop Hub

Correction:
bind multi-size ICO, PNG and SVG assets to executable, popup and tray fallback path

Public checkpoint:
7418406
```

### Portable release tooling

```text
Symptom:
release helper depended on static repository assumptions

Correction:
dynamic root-solution discovery
dynamic smoke-test discovery
process working-directory anchoring
expanded Git-status inspection
clean-extraction executable self-test

Public checkpoint:
71a10a5
```

## Release-publication rule

A pushed commit is necessary but not sufficient for an externally meaningful milestone.

For every externally meaningful validated checkpoint:

```text
commit
push
verify origin/main
update README summary
update CHANGELOG detail
update this history document
create or update Git tag
publish GitHub Release promptly
attach portable artifacts where the checkpoint is distributable
```

## Public-private boundary

Keep public:

```text
objective architecture history
objective API history
sanitized debug history
release notes
tags
GitHub Releases
README summary
CHANGELOG details
```

Keep private:

```text
credentials
tokens
private Owner instructions
machine-specific private paths
private logs
unredacted diagnostics
internal constitutions
private failure-analysis case studies
```
