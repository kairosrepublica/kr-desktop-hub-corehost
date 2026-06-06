# KR Desktop Hub CoreHost

Portable-first and extensible desktop CoreHost for Windows 11 Widgets.

## Current status

```text
Project stage: CoreHost v0.2.1-rc1 frozen baseline
Runnable application: implemented and locally validated
CoreHost panel: universal adaptive WidgetHostSurface foundation in progress
Production Widgets: begin only after universal CoreHost Widget framework validation
Canonical public checkpoint before this documentation sync: dabca8f
```

The CoreHost foundation is complete enough to begin production Widget development. Future user-facing functions remain independent Widgets rather than embedded CoreHost business logic.

The first Phase 1 production-Widget sequence is:

```text
Widget 01: KR World Time-Space
Widget 02: KR Trading Clock
```
## Target platform

Initial implementation target:

```text
Windows 11 x64
```

Planned compatibility paths are reserved for:

```text
Windows ARM64
Apple macOS
```

Those platforms are **not implemented or supported yet**.

## Distribution principle

Portable ZIP first:

```text
extract
run
```

A simple installer may be added later. Portable mode must remain available.

## Start here

Run:

```powershell
.\START_HERE.ps1
```

Only `START_HERE.ps1` is exposed as the normal operator entry point. Advanced scripts remain under `tools\advanced\`.

## Documentation

Start with:

```text
docs\Product_Scope.md
docs\Architecture.md
docs\ROADMAP_IMPLEMENTATION.md
docs\governance\PUBLIC_DEVELOPMENT_RECORD_POLICY.md
```

## Security

Never commit secrets, private logs, personal calendar data, local configuration, API keys, tokens, private certificates or machine-specific private paths.

See:

```text
SECURITY.md
```

## License

No open-source license has been selected yet.

A public portfolio repository and a permission grant for third-party reuse are separate decisions.


## Canonical GitHub repository

The single canonical public repository target is:

```text
kairosrepublica/kr-desktop-hub-corehost
```

Commits are authored through the Kent Reis personal GitHub identity:

```text
kentreis
```

Do not create a competing primary repository under the personal account.

## Portable release candidate

Build and validate the local Windows 11 x64 portable release candidate:

```powershell
.\tools\BUILD_VERIFY_PORTABLE_RELEASE.ps1
```

The generated ZIP, SHA-256 file and resource baseline remain under:

```text
dist/releases/
```

Release binaries are local artifacts until manual desktop acceptance is complete.

## Public checkpoint discipline

Every validated, Owner-approved engineering step is committed and pushed immediately as a scoped public checkpoint. Public narrative evidence is updated in the same checkpoint whenever the product state changes.

See:

```text
docs\governance\PUBLIC_DEVELOPMENT_RECORD_POLICY.md
```

## Phase 1 final HTML interaction gate

Final Owner-review prototype:

```text
prototypes/phase1-review-shell/v0.7
```

Independent Widget prototypes:

```text
prototypes/world-time-space-widget/v0.7
prototypes/trading-clock-widget/v0.5
```

Frozen interaction targets:

```text
World Time-Space default height:
220 DIP

Trading Clock default height:
500 DIP

World Time-Space:
row-based auto-height growth
right-click Add city...
floating Add City chooser
right-click Remove city

Trading Clock:
approved v0.5 visual baseline
collapse-based auto-height shrink in the host review shell
```

Widget release distribution:

```text
GitHub downloadable Widget releases use an outer AES-256 encrypted .7z archive.
To request free authorization and the extraction password,
email kr@kairosrepublica.com.
```


## Universal Widget framework foundation

The first CoreHost framework extension establishes:

```text
600 DIP default popup width
Widget width inherited from CoreHost
measured adaptive height
collapsed, expanded and disabled host states
host-level overflow scrolling only when required
CoreHost-owned UI design tokens
CoreHost-owned floating-dialog broker
CoreHost-owned tray-icon request broker
approved declarative tray visual states
```

Widgets remain isolated packages. They may consume the versioned CoreHost framework but must not import one another's code, styles, settings or mutable runtime state.
