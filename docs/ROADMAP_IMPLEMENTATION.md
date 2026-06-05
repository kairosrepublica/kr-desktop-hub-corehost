# Implementation Roadmap

## Frozen CoreHost baseline

Version:

```text
v0.2.1-rc1
```

Code checkpoint before the public narrative-sync commit:

```text
dabca8f78f26918ebbf0e543175013b46cf244f3
```

The CoreHost architecture is frozen unless production Widget development exposes a real blocking defect.

## Phase 1 Widget sequence

### Checkpoint 1 â€” Public CoreHost narrative synchronization

Scope:

- synchronize README;
- synchronize changelog;
- synchronize public roadmap;
- add public development-record policy;
- tag the frozen public baseline.

### Checkpoint 2 â€” Phase 1 HTML/CSS interaction prototype

Scope:

- KR World Time-Space prototype;
- KR Trading Clock prototype;
- Owner review;
- interaction-specification freeze.

### Checkpoint 3 â€” KR World Time-Space specification

Scope:

- freeze requirements;
- freeze offline holiday-pack boundary;
- freeze user-facing layout;
- define tests.

### Checkpoint 4 â€” KR World Time-Space implementation

Scope:

- implement production Widget;
- package `.krwidget.zip`;
- validate installation;
- run regression tests;
- complete manual acceptance.

### Checkpoint 5 â€” KR Trading Clock specification

Scope:

- freeze exchange-local timelines;
- freeze user-local timelines;
- freeze annual market-calendar boundary;
- define tests.

### Checkpoint 6 â€” KR Trading Clock implementation

Scope:

- implement production Widget;
- package `.krwidget.zip`;
- validate installation;
- run regression tests;
- complete manual acceptance.

### Checkpoint 7 â€” Phase 1 release

Scope:

- publish Phase 1 release notes;
- publish validated packages;
- update public roadmap;
- tag milestone;
- publish GitHub Release when approved.

## Atomic GitHub rule

Every checkpoint above is committed and pushed immediately after validation.

If a checkpoint contains several independently meaningful engineering steps, split it into smaller atomic checkpoints rather than delaying synchronization.
