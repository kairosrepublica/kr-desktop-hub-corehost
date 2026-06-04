# Architecture Baseline

## CoreHost responsibilities

- Application lifecycle
- Single-instance execution
- System-tray lifecycle
- Startup configuration
- Configuration persistence
- Localization interface reservation
- Widget discovery
- Widget loading
- Widget failure isolation
- Logging

## Widget responsibilities

- Implement an individual user-facing function
- Use the documented Widget SDK
- Avoid direct control of the CoreHost lifecycle
- Fail without crashing unrelated widgets
- Declare required permissions explicitly
