# Roadmap

## Phase 1: Portable CoreHost shell

- Create the Windows 11 x64 application shell.
- Enforce single-instance execution.
- Add system-tray lifecycle management.
- Add explicit exit behavior.

## Phase 2: Startup behavior

- Add configurable startup after login.
- Add configurable startup delay.
- Keep the panel hidden after login by default.
- Minimize the window to the system tray on close.

## Phase 3: Configuration governance

- Add configuration persistence.
- Add recommended defaults and explanations.
- Reserve localization interfaces.
- Define administrator-privilege boundaries.

## Phase 4: Widget platform

- Define Widget SDK lifecycle hooks.
- Add a HelloWidget example.
- Add widget discovery and failure isolation.
- Produce Widget API documentation.

## Phase 5: Quality and distribution

- Add automated Windows x64 build checks.
- Add tests.
- Produce portable ZIP packages.
- Validate clean extraction and execution on another Windows 11 computer.
