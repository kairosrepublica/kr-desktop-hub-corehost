# Changelog

All notable changes to KR Desktop Hub CoreHost will be documented in this file.

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
