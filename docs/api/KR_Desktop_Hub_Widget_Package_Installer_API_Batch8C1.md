# KR Desktop Hub Internal Widget Package Installer API

## Batch identity

`Stabilization Batch 8C1`

## Purpose

This batch creates the CoreHost foundation for installing Owner-approved internal Widgets through one validated pipeline.

It does not implement the Widget Manager user interface and does not install or execute any production Widget.

## Production package format

Production packages must use:

```text
<name>.krwidget.zip
```

A ZIP file with any other extension is rejected.

Every package must contain a root-level:

```text
manifest.json
```

## Default data-root directories

```text
plugins/
  inbox/
  installed/
  staging/
  backups/
  quarantine/
```

## Security rules

The installer:

1. discovers only `.krwidget.zip` files in `plugins/inbox`;
2. never auto-installs or auto-runs a dropped file;
3. stages every package before installation;
4. rejects path traversal and absolute paths;
5. rejects duplicate archive paths;
6. enforces file-count and expanded-size limits;
7. validates the root manifest;
8. checks CoreHost compatibility;
9. checks requested capabilities against the allowlist;
10. verifies that the declared DLL exists;
11. backs up an existing installed Widget before replacement;
12. moves the staged payload into place only after validation;
13. rolls back the prior installation if replacement fails;
14. copies rejected archives into quarantine with a reason file.

## Development-folder installation

Advanced development-only folder installation exists as an internal API, but it is disabled by default.

It uses the same staged validation pipeline and rejects file-system reparse points.

## Current boundary

This batch creates the validated installer foundation only.

The next batch will connect this installer to an internal Widget Manager interface, file picker and explicit `plugins/inbox` discovery action. Arbitrary dropped files will remain inert until the Owner explicitly chooses an install action.