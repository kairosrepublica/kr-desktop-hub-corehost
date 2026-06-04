# KR Desktop Hub Diagnostics and Migration API â€” Batch 6 Baseline

## Purpose

Batch 6 adds low-friction diagnostics export and cross-computer data migration.

## Diagnostics

```text
StructuredFileDiagnosticLogger
DiagnosticTextRedactor
JsonSecretRedactor
DiagnosticExportOptions
DiagnosticsExporter
```

Recommended diagnostic export:

```text
include sanitized JSON configuration
include log file names
exclude log contents by default
include process runtime snapshot
```

## Migration

```text
DataMigrationOptions
DataMigrationManifest
DataMigrationImportResult
PortableDataMigrationService
```

Recommended migration export:

```text
include config
include state
include plugins
exclude logs by default
exclude cache by default
create a backup before every import
```

## Archive safety

Migration import validates every ZIP entry before extraction.

Entries that escape the staging directory or use unsupported top-level directories are rejected.

## Status

Batch 6 baseline.