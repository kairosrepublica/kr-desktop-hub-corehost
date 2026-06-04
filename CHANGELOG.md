# Changelog

All notable changes to KR Desktop Hub CoreHost will be documented in this file.

The project uses meaningful GitHub checkpoints. Changes should represent coherent engineering units rather than artificial commit volume.

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
