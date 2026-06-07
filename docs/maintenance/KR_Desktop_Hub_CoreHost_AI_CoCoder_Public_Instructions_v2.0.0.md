# KR Desktop Hub CoreHost AI Co-Coder Public Instructions v2.0.0

## Read first

Before proposing any CoreHost code change, read:

```text
README.md
docs/Product_Scope.md
docs/Architecture.md
docs/maintenance/KR_Desktop_Hub_CoreHost_Maintainer_Handoff_v2.0.0.md
docs/api/KR_Desktop_Hub_API_Index.md
```

## Do not violate these boundaries

```text
Do not add production Widget logic to CoreHost.
Do not couple ordinary state-only mutations to filesystem discovery.
Do not mutate candidate catalog state before acceptance.
Do not let Widgets own the Windows tray icon.
Do not resize the outer popup because a Widget changes height.
Do not bypass full build, smoke tests or clean-extraction self-test.
Do not bundle unrelated engineering scopes into one commit.
```

## CoreHost versus Widgets

Production Widget work belongs in:

```text
kairosrepublica/kr-desktop-hub-widgets
```

CoreHost may keep only sample and regression fixtures.

## API work

When changing Widget-facing contracts:

```text
update Contracts
update Widget SDK
update schema where required
update Widget developer API docs
update API surface map
add compatibility tests
document migration impact
```

## Windows shell work

When changing popup focus, activation, taskbar behavior, shadow or input-method editor behavior:

```text
perform static audit
add diagnostics
run automated gates
require Owner manual Windows replay
```

## Public narrative

Update README, CHANGELOG, ROADMAP and relevant docs in the same engineering checkpoint when public state changes.
