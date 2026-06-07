# Public Release and Iteration-History Policy

## Purpose

This repository is both a working CoreHost repository and a public, sanitized engineering record.

A pushed commit records source history. A GitHub Release records a publicly consumable milestone. Both are required when a validated checkpoint changes the externally meaningful product state.

## Mandatory GitHub Release rule

For every externally meaningful validated checkpoint:

```text
create or update the Git tag promptly
publish the corresponding GitHub Release promptly
write release notes
mark pre-release versus stable status truthfully
attach portable artifacts when the checkpoint is distributable
verify the Release page after publication
```

Do not leave a meaningful validated milestone represented only by an internal conversation, a local ZIP or a pushed commit.

## Historical backfill rule

When a repository discovers that meaningful historical tags or milestones exist without GitHub Releases:

```text
backfill Releases
preserve truthful dates and commit targets
mark historical release candidates as pre-releases
do not pretend that an old pre-release was a stable public release
```

## README synchronization

`README.md` must contain:

```text
current delivered or release-candidate status
latest download instruction
repository boundary
release-history summary
sanitized development-history summary
sanitized debug-history summary
links to detailed public history
```

## CHANGELOG synchronization

`CHANGELOG.md` must contain detailed, chronological product changes.

Do not leave externally delivered work indefinitely under an `Unreleased` heading.

## Detailed history document

Maintain:

```text
docs/history/KR_Desktop_Hub_CoreHost_Public_Development_And_Debug_History.md
```

Use it for the complete sanitized iteration and debug timeline.

## Issues, tags and Releases

Use public GitHub evidence according to purpose:

```text
Issues:
planned work, known gaps and follow-up scope

Commits:
atomic implementation history

Tags:
immutable milestone pointers

Releases:
publicly consumable milestone pages and downloadable artifacts

README:
current summary and condensed history

CHANGELOG:
detailed change record

docs/history:
complete sanitized development and debug narrative
```

## Public-private boundary

Never publish private logs, credentials, tokens, machine-specific private paths, internal constitutions or private Owner instructions.
