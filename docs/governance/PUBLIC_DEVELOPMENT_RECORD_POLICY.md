# Public Development Record Policy

## Purpose

This repository is both a working engineering repository and a public, sanitized development record for KR Desktop Hub.

The public record must remain truthful, atomic and understandable. It must show how the product evolved without exposing private local material.

## Atomic checkpoint rule

Every Owner-approved engineering step that changes the public project state must create one scoped Git commit immediately after validation.

Each validated public checkpoint must be pushed to the canonical `origin/main` branch immediately. Do not wait until the end of a large batch to combine several independently meaningful engineering steps into one public checkpoint.

Do not manufacture empty commits or artificial commit volume. A checkpoint must correspond to a real, reviewable change.

## Mandatory Release synchronization

A pushed commit is necessary but not sufficient for an externally meaningful milestone.

For every validated checkpoint that changes the externally meaningful product state:

```text
create or update the Git tag promptly
publish or update the GitHub Release promptly
write truthful release notes
mark release-candidate status as pre-release
attach distributable artifacts when available
verify the GitHub Releases page
```

Historical milestones that were tagged or functionally delivered without a GitHub Release must be backfilled truthfully.

## Required narrative synchronization

When a step changes the public understanding of the product, update the relevant public narrative evidence in the same checkpoint:

- `README.md`
- `CHANGELOG.md`
- `ROADMAP.md`
- `docs/ROADMAP_IMPLEMENTATION.md`
- `docs/history/KR_Desktop_Hub_CoreHost_Public_Development_And_Debug_History.md`
- public Issues
- milestone tags
- GitHub Releases

`README.md` must retain a readable release-history summary and a sanitized debugging-history summary. The detailed iteration narrative belongs in `docs/history`.

## Public-private boundary

Public GitHub may contain:

- source code;
- test code;
- sanitized example configuration;
- objective architecture documentation;
- objective API documentation;
- public release notes;
- sanitized development and debugging history;
- public development-record policy.

Do not commit:

- credentials, tokens or secrets;
- machine-specific private paths;
- private logs;
- local-only diagnostics;
- Owner instructions;
- internal constitutions or SOPs;
- private failure-analysis case studies;
- unredacted personal data;
- `owner_private_docs/`.

## Evidence required after each pushed checkpoint

Record:

- commit SHA;
- commit message;
- files changed;
- validation gates passed;
- remote synchronization result;
- narrative files updated;
- Issue, tag and Release changes;
- attached distribution artifacts where applicable.
