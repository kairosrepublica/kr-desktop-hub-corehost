# KR Desktop Hub CoreHost

Portable-first and extensible desktop CoreHost for Windows 11 Widgets.

## Current status

```text
Project stage: CoreHost v0.2.1-rc1 frozen baseline
Runnable application: implemented and locally validated
CoreHost panel: universal adaptive WidgetHostSurface foundation implemented
Installed Widget catalog: package-to-runtime manifest adapter implemented
Production Widgets: begin after Widget Management UI composition and activation wiring
Canonical public checkpoint before this engineering step: 529d342
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


## Installed Widget activation boundary

The CoreHost now maintains one explicit translation boundary between:

```text
installed .krwidget.zip package manifest
snake_case package fields
```

and:

```text
runtime Widget manifest
camelCase runtime fields
```

The installed-Widget catalog reads top-level installed package folders, maps schema-1 package metadata into runtime metadata, registers adaptive host-state defaults and exposes backend enable, disable, collapse, expand and ordering controls.

The next CoreHost checkpoint wires this backend into:

```text
Widget Management inventory UI
production WidgetHostSurface composition
CoreHost-owned floating-dialog presenter
Windows tray-icon broker application
```

## CoreHost Checkpoint 2B — Windows Widget composition

CoreHost now wires installed Widget inventory into the Windows panel and Widget Management window. Disabled Widgets can be reopened from Widget Management. Expanded and collapsed states persist through the shared host-state store. The Windows panel composes generic isolated Widget cards at the 600-DIP baseline and exposes a registry seam for package-specific visual surfaces. CoreHost also owns the floating-dialog presenter and applies governed tray-icon selections through the Windows tray service.

## CoreHost stabilization gate

Owner manual acceptance exposed two CoreHost defects before production Widget development:

```text
clearing both quiet-hours fields repopulated the prior values
collapsing a Widget card resized the outer popup window
```

The stabilization checkpoint separates editor intent from runtime fallback values and separates internal Widget-card height changes from the outer popup viewport policy.

The Settings Center recommended quiet-hours state is internally valid before the runtime bridge runs: `Enabled = true`, `Start = 23:00`, `End = 08:00`. Clearing both fields remains an explicit disable action.

The outer popup now preserves or grows its height. It does not automatically shrink because an individual Widget was collapsed.


## CoreHost stabilization gate — window snap and Widget refresh integrity

Additional Owner acceptance testing exposed two deeper host-boundary failures:

```text
vertically snapped outer popup height was reduced when a Widget collapsed
an overlapping or degraded installed-catalog refresh could temporarily replace the visible panel with the empty state
```

The follow-up stabilization checkpoint preserves user- or Windows-expanded outer geometry unless automatic growth is required, serializes host mutations and catalog refreshes, rejects degraded catalog snapshots that would remove a previously visible Widget, and builds Widget cards transactionally before replacing the visible panel.

## Owner-sized CoreHost popup viewport

The CoreHost popup is an Owner-controlled viewport.

```text
default width:
600 DIP

minimum width:
600 DIP

allowed:
Owner may widen the popup
Owner may adjust popup height

forbidden:
Widget Expand may not change outer width or height
Widget Collapse may not change outer width or height
Widget refresh may not reset an Owner-adjusted width or height

overflow:
use the CoreHost ScrollViewer
do not auto-resize the outer popup
```
