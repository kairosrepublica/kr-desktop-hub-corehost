# ADR-0004: Public Sanitized Configuration and Private Local Override

## Status

Accepted.

## Decision

Commit sanitized configuration examples only.

Keep Owner-specific runtime paths, credentials, logs and personal configuration local and untracked.

## Reason

The repository is intended to be public.

A credible public development record must not expose private configuration.

## Consequences

The public example uses a generic data root.
Owner-specific overrides remain local.
