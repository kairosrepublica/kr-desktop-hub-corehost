# ADR-0003: Platform Abstraction Boundary

## Status

Accepted.

## Decision

Windows-specific services remain behind platform abstractions.

## Reason

Version 0.1 targets Windows 11 x64, but the architecture must reserve paths for Windows ARM64 and Apple macOS.

## Consequences

WPF and Win32 types must not leak into platform-neutral Contracts.
