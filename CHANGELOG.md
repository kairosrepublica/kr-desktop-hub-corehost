# Changelog

All notable changes to KR Desktop Hub CoreHost will be documented in this file.

## [2.0.0] - 2026-06-07 — Delivered CoreHost platform

### Added

- Added the default branded KR CoreHost icon for the executable, popup window and tray default visual state.
- Added explicit non-disruptive popup policy: `ShowActivated = false`, `ShowInTaskbar = false`, and no forced `Activate()` after ordinary Show.
- Added sanitized `shell.panel.lifecycle` diagnostic records with popup visibility, active state, focused-element type, bounds, working-area bounds, `Topmost`, `ShowActivated` and `ShowInTaskbar`.
- Added CoreHost v2.0.0 release notes and Owner manual shell-acceptance checklist.
- Added public CoreHost maintainer handoff and AI co-coder instructions.
- Added Widget developer API v2.0.0 and CoreHost API surface map.
- Added explicit CoreHost and production-Widgets repository separation architecture note.
- Upgraded portable release tooling to discover and run every smoke-test project dynamically.
- Hardened portable release tooling with parser-safe relative-path discovery, dynamic root-solution discovery, process working-directory anchoring and expanded Git-status inspection.
- Added tray-popup native `WS_EX_NOACTIVATE` policy after Owner replay proved that WPF `ShowActivated = false` alone did not eliminate Microsoft Pinyin focus disturbances.
- Added minimize-to-tray interception so the title-bar minimize command hides the popup and leaves only the tray icon.
- Made the standard Collapse / Expand control non-focusable to avoid unnecessary keyboard-focus transitions inside the tray popup.

### Changed

- Raised the internal Widget-package host compatibility version to `2.0.0`.
- Removed production Widget prototypes, production Widget requirements and the production Widget placeholder from the CoreHost repository. They move to the separate `kairosrepublica/kr-desktop-hub-widgets` repository.
- Updated public README, Product Scope, Architecture, Roadmap and API index for the v2.0.0 platform boundary.

### Manual acceptance completed

- Verify no Microsoft Pinyin Chinese / English indicator jump.
- Verify no taskbar notification-area darkening.
- Verify no visually dirty lower-edge shadow overlap.
- Verify popup remains manually interactive.
- Verify Owner geometry persistence and host-level scrolling.

## CoreHost state-only Widget-host transition checkpoint

- Removed installed-package filesystem rediscovery from normal Collapse, Expand, enable, disable and order operations.
- Added a framework-owned Widget-chrome transition seam and sample-card consumption path.
- Added accepted-catalog state projection so state-only mutations update the in-memory host surface without invoking degraded catalog-snapshot evaluation.
- Preserved Widget card and visual-surface instances during state-only reconciliation.
- Centralized title-bar close-to-tray through the CoreHost hide path so system-policy visibility remains synchronized.
- Unified the persisted popup minimum-width contract at 600 device-independent pixels and the minimum-height contract at 240 device-independent pixels.
- Added regression coverage for framework chrome, 50 serialized Collapse / Expand transitions, state-only projection and geometry constants.
- Production business Widgets remain blocked pending the remaining CoreHost stabilization checkpoints and Owner manual acceptance.

The project uses meaningful GitHub checkpoints. Changes should represent coherent engineering units rather than artificial commit volume.

## [0.2.1-rc1] - 2026-06-05

### Added

- Public atomic-checkpoint and narrative-synchronization policy.
- Phase 1 production-Widget roadmap for KR World Time-Space and KR Trading Clock.

### Changed

- Public project status now reflects the frozen CoreHost `v0.2.1-rc1` baseline.
- Public roadmap now separates the frozen CoreHost platform from Phase 1 Widget development.
- The popup panel is documented as an intentionally blank `WidgetHostSurface` reserved for production Widgets.

### Governance

- Every validated Owner-approved engineering step must create a scoped commit and immediate push.
- Narrative evidence must be updated in the same checkpoint whenever public project state changes.
- Empty commits and artificial commit volume remain prohibited.
## [Unreleased]

### Added

- Installed Widget catalog backend with top-level installed-folder discovery.
- Package-to-runtime manifest adapter for schema-1 `.krwidget.zip` manifests.
- Widget Manager backend controls for installed inventory, enable, disable, collapse, expand and ordering.
- Runtime-loader support for both legacy camelCase development manifests and installed snake_case package manifests.
- Smoke-test coverage for installed inventory controls and installed-package runtime adaptation.
- Universal CoreHost Widget-framework foundation.
- `600 DIP` default popup-width baseline.
- Adaptive Widget layout controller with measured expanded height, collapsed height, disabled state and host-level overflow fallback.
- CoreHost-owned Widget UI token resource dictionary.
- Governed floating-dialog broker contract.
- Governed tray-icon request broker with approved-state registry, priority, expiry and fallback handling.
- Regression coverage for invalid Widget presentation metadata, unknown tray-icon states, expired tray-icon requests, missing tray-icon capability, priority arbitration and fallback restoration.
- Solution-completeness gate for every discovered smoke-test project: solution registration, in-section Release build mapping and individual pre-run build verification.
- Additional deny-by-default brokered capabilities for UI surfaces, height reporting, state, settings, context menus, dialogs, tray icons and diagnostics.
- Widget-framework smoke tests.

### Changed

- Widget package manifest foundation now accepts optional presentation and state-schema metadata while preserving schema-1 compatibility.
- Windows tray adapter now maps declarative CoreHost visual states to approved built-in icons.


### Fixed

- Preserved user- or Windows-expanded outer popup height when collapsing a Widget near a screen edge; the automatic work-area cap now constrains growth only and never shrinks an already-expanded shell.
- Serialized installed-catalog refreshes and host mutations to prevent overlapping collapse, expand and refresh operations from rendering stale transient states.
- Retained the last known-good Widget panel when a degraded catalog refresh would temporarily remove a previously visible Widget.
- Built Widget cards transactionally before replacing the visible host surface.

- Hardened Windows smoke-test teardown after unloading collectible Widget assembly contexts: isolate temporary loaded assemblies, run bounded cleanup retries and emit success only after teardown passes.
- Corrected installed review-shell iframe paths for KR World Time-Space and KR Trading Clock.
- Added installed-topology path-resolution validation before opening the review shell.

### Added

- Phase 1 final HTML interaction gate.
- KR World Time-Space `v0.7` prototype with `220 DIP` default height and automatic row-based growth.
- World Time-Space root context menu with `Add city...`.
- Floating Add City chooser above the complete popup review shell.
- World Time-Space city-card context menu with `Remove city`.
- KR Trading Clock approved `v0.5` prototype, preserved byte-for-byte.
- Host-review-shell demonstration of Trading Clock height shrink after market collapse and restoration after expansion.
- Public encrypted Widget-release distribution policy.

### Governance

- Public downloadable Widget releases use an outer AES-256 encrypted `.7z` archive.
- Users request free authorization and the extraction password by emailing `kr@kairosrepublica.com`.

### Added

- Batch 0 repository bootstrap.
- Canonical folder structure.
- Single public launcher: `START_HERE.ps1`.
- Public product-scope baseline.
- Public architecture baseline.
- Architecture Decision Records for CoreHost separation, portable-first distribution, platform abstraction and sanitized public configuration.
- GitHub bootstrap and initial-Issue helper scripts.
- Batch 0 structure-verification script.
- Deferred Batch 1 `.NET` project-initialization script.
- Private local-only handoff folder excluded from Git.


## [0.0.2] - 2026-06-04

### Changed

- Replaced the personal repository target with the canonical Organization repository: `kairosrepublica/kr-desktop-hub-corehost`.
- Split first-upload automation into five fail-fast, independently verifiable stages.
- Added mandatory backup of any previous partial `.git` history before reinitialization.
- Added explicit ID-based GitHub noreply-email validation.
- Added post-push verification of repository identity, uploaded files, author identity and local status.
- Updated local-only governance handoff to v2.0.

## 0.1.0-rc1

Added the first local portable release-candidate workflow for Windows 11 x64.

The workflow builds, runs all smoke tests, publishes a self-contained application, assembles a portable ZIP, validates clean extraction, runs the extracted self-test, starts the hidden tray host and records a Proof-of-Concept resource baseline.

## Unreleased — CoreHost Checkpoint 2B

- Added Widget Management installed-inventory UI with enable, disable, expand, collapse and ordering controls.
- Added shared persistent Windows Widget-host composition at the 600-DIP popup baseline.
- Added CoreHost-owned floating-dialog presenter.
- Wired governed Widget tray-icon selections into the Windows tray service.
- Added a non-breaking integrated Widget-context extension seam for future isolated Widget packages.
## Unreleased — CoreHost stabilization after Owner manual acceptance

### Fixed

- Quiet-hours settings can now be cleared intentionally: clearing both fields disables quiet hours and remains blank after save, reload and reopening Settings Center.
- Widget-card collapse now reduces only the Widget card and internal desired layout height; the outer CoreHost popup uses a preserve-or-grow viewport policy and no longer shrinks merely because one Widget was collapsed.

### Validation

- Added runtime-bridge regression coverage for quiet-hours clear, reload and explicit re-enable.
- Added Settings Center validation for partial quiet-hours pairs and enabled-but-blank quiet-hours state.
- Added default-state closure coverage: the recommended quiet-hours default is now a valid 23:00–08:00 pair before any runtime overlay is applied.
- Added Widget-host viewport regression coverage for collapse isolation and host-level overflow fallback.

## CoreHost stabilization — Owner-sized popup viewport

- Locked the CoreHost normal-popup minimum width at 600 DIP.
- Removed Widget-driven outer width resets: Owner-expanded widths remain intact.
- Removed Widget-driven outer height changes: Expand and Collapse now affect Widget cards only.
- Routed vertical overflow through the CoreHost ScrollViewer instead of resizing the popup.
- Preserved the alpha5 serialized catalog refresh, last-known-good snapshot retention and transactional panel replacement protections.

## Unreleased — CoreHost stabilization: transactional installed-catalog refresh

- Added pure staged installed-Widget discovery: manifest scans no longer mutate active host registrations before candidate acceptance.
- Added exact accepted-catalog reconciliation with active-registration and measured-height pruning while preserving dormant Owner host-state preferences for later reinstall.
- Strengthened degraded-candidate protection so discovery failures cannot silently remove previously accepted disabled Widgets.
- Added exact capability approval reconciliation and host-owned revocation of stale tray-icon requests.
- Routed Widget Manager explicit refreshes through one accepted-catalog coordinator pipeline.
- Subscribed the Windows host to framework `LayoutChanged` so desired-height reports refresh host-level scrolling without package discovery or outer-popup resize.


## Public GitHub Release-governance correction — 2026-06-07

### Fixed

- Backfilled GitHub Releases for meaningful historical CoreHost milestones that were previously represented by commits or tags without corresponding GitHub Release pages.
- Published `v2.0.0` as the stable portable CoreHost delivery.
- Added a public release-history summary and sanitized debugging-history summary to `README.md`.
- Added a complete sanitized public development and debug-history document.
- Strengthened public governance so externally meaningful checkpoints must publish Git tags and GitHub Releases promptly rather than stopping at commits.
