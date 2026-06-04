# ADR-0005: Organization Canonical Repository and Staged First Push

## Status

Accepted

## Decision

Use one canonical public repository:

```text
kairosrepublica/kr-desktop-hub-corehost
```

Commits are authored by Kent Reis through the `kentreis` personal GitHub identity.

Do not maintain a competing personal primary repository.

Split the first upload into independently verifiable stages:

```text
1. preflight
2. backup and local reinitialization
3. clean local baseline commit
4. Organization repository creation and push
5. upload verification
```

## Reason

The project must produce both:

```text
personal developer evidence
company product evidence
```

One Organization-owned canonical repository with personal commits satisfies both goals without creating ambiguous duplicate repositories.

Staged execution prevents cascading errors and preserves the incomplete previous `.git` history before reset.

## Consequences

- Public Issues and Releases live under the Organization repository.
- Previous incomplete local Git history is backed up before reset.
- No step assumes a remote repository exists.
- No push is considered complete before verification.
