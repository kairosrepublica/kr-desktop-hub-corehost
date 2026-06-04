# Security Policy

## Public repository boundary

This public repository must never contain:

```text
.env
.env.*
secrets/
credentials/
config.local.*
*.local.json
config.json containing real values
*.key
*.pem
*.pfx
*.p12
tokens/
API keys
access tokens
passwords
certificates
private keys
personal calendar data
personal email data
private logs
administrator authorization credentials
machine-specific private configuration
owner_private_docs/
```

## Commit sanitized examples only

Use:

```text
config/config.example.json
```

Do not commit the Owner's real configuration.

## Mandatory staged-change inspection

Before each push:

```powershell
git status --short
git diff --cached --stat
git diff --cached --check
```

## Version 0.1 Widget trust boundary

Version 0.1 allows only Owner-approved internal Widgets developed against the KR Desktop Hub API.

Third-party Widgets are not allowed.

Arbitrary Widget shell execution and external-script execution are disabled by default.
