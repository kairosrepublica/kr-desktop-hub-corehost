# KR Desktop Hub Widget Capability Governance API

## Batch identity

`Stabilization Batch 8D1`

## Purpose

This API establishes a default-deny capability boundary for future internal Widgets.

A Widget cannot use a sensitive CoreHost capability merely because it declares a string in its package manifest. Access requires:

1. a known capability identifier;
2. a brokered capability that is enabled in the current CoreHost release;
3. declaration by the Widget package;
4. explicit approval for the Widget;
5. access through a governed broker interface.

## Capability catalog

| Capability | Current disposition |
|---|---|
| `clock.read` | Brokered |
| `notification.send` | Brokered |
| `network.http` | Reserved; unavailable |
| `calendar.read` | Reserved; unavailable |
| `file.read.scoped` | Reserved; unavailable |
| `file.write.scoped` | Reserved; unavailable |
| `shell.execute` | Prohibited |
| `script.execute` | Prohibited |

## Decision codes

```text
Allowed
UnknownCapability
ProhibitedCapability
ReservedCapabilityUnavailable
NotDeclared
NotApproved
```

## Package-installer integration

The package installer now accepts only capabilities that are both:

- brokered in the current CoreHost release;
- included in the installer allowlist.

Reserved, prohibited and unknown capabilities are rejected during package validation.

## Audit boundary

Every authorization decision is sent to an audit sink. The initial in-memory sink is intended for runtime diagnostics and future durable audit-log integration.