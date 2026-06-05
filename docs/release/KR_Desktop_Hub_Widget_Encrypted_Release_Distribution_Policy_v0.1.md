# KR Desktop Hub Widget Encrypted Release Distribution Policy v0.1

## Scope

This policy applies to public GitHub downloadable release artifacts for KR Desktop Hub Widgets.

## Internal installable package

The validated internal package format remains:

```text
.krwidget.zip
```

This preserves the controlled CoreHost Widget Manager installation pipeline.

## GitHub downloadable artifact

The public GitHub release must not expose the internal package directly.

Publish an outer encrypted archive:

```text
format:
.7z

encryption:
AES-256
```

The encrypted archive contains:

```text
validated .krwidget.zip
SHA-256 checksum file
authorization notice
```

## Authorization request

Public release notes and README text must state:

```text
To request free authorization and the extraction password,
email kr@kairosrepublica.com.
```

## Secret handling

Never commit or publish:

```text
archive password
password hint that materially reveals the password
password inside scripts
password inside CI logs
password inside release notes
password inside Issues
password inside source code
```

Password distribution is manual and private.

## Public-source clarification

This policy encrypts downloadable compiled Widget release artifacts.

It does not automatically make a public source repository private. Source-visibility policy is a separate Owner decision.
