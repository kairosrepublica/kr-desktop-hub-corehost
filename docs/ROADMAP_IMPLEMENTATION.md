# KR Desktop Hub CoreHost — Implementation Roadmap

## Current release target

```text
CoreHost:
v2.0.0

Target:
Windows 11 x64 portable ZIP

Status:
release candidate pending Owner shell manual acceptance
```

## Completed checkpoint history

### Batch A — state-only Widget-host transitions

```text
ordinary Collapse / Expand is discovery-free
Enable / Disable is discovery-free
Move Up / Move Down is discovery-free
accepted-catalog projection added
card instances preserved
600-DIP minimum width frozen
240-DIP minimum height frozen
```

### Batch B — transactional catalog refresh

```text
pure staged discovery
acceptance before mutation
exact active-registration reconciliation
stale measured-height pruning
dormant preference preservation
stronger degraded-candidate rejection
exact capability reconciliation
stale tray-request revocation
LayoutChanged host subscription
Widget Manager single accepted-refresh pipeline
```

### Shell release-candidate checkpoint

```text
ShowActivated = false
ShowInTaskbar = false
ordinary Show no longer forces Activate()
close-to-tray remains centralized
shell lifecycle diagnostics added
public v2.0.0 docs added
Widget-facing API docs added
release tooling upgraded to discover all smoke tests
production Widget prototypes removed from CoreHost repository
```

## Final v2.0.0 publication sequence

```text
1. Run complete automated gates.
2. Push shell release-candidate commit.
3. Generate portable v2.0.0 ZIP.
4. Run Owner Windows shell manual acceptance.
5. Tag v2.0.0.
6. Publish GitHub release with ZIP and SHA-256.
7. Bootstrap separate kr-desktop-hub-widgets repository.
```

## Repository separation

CoreHost retains:

```text
Contracts
Widget SDK
HelloWidget sample
fixture Widgets
tests
public API docs
```

Separate Widgets repository owns:

```text
production Widget requirements
interaction prototypes
production Widget code
Widget release artifacts
Widget-specific tests
Widget-specific roadmap
```
