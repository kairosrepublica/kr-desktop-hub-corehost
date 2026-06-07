# KR Desktop Hub CoreHost Windows Shell Stabilization v1.0

## Scope

This checkpoint closes the remaining low-risk Windows shell defects before CoreHost `v2.0.0` release.

## Observed defects

```text
popup open or close disturbed the visible Microsoft Pinyin Chinese / English indicator
popup open darkened the taskbar notification-area region
popup lower-edge shadow looked visually dirty near the taskbar
```

## Static source findings before correction

```text
MainWindow had no explicit ShowActivated policy
MainWindow had no explicit ShowInTaskbar policy
ordinary App.ShowPanel called _panel.Show()
then
ordinary App.ShowPanel called _panel.Activate()
```

The source did not contain:

```text
custom WindowChrome
AllowsTransparency
custom drop-shadow layer
explicit input-method editor mutation
```

## Selected low-risk correction

```text
ShowActivated = false
ShowInTaskbar = false
ordinary popup Show does not force Activate()
```

This is deliberately narrower than applying `WS_EX_NOACTIVATE`.

A stronger Win32 no-activate style is deferred because it can prevent activation even when the Owner manually clicks the popup and may create accessibility or interaction regressions.

## Shell lifecycle

All ordinary hide paths use the centralized CoreHost lifecycle:

```text
HidePanel(reason)
synchronize system-policy visibility
write sanitized shell lifecycle diagnostic
```

All ordinary show paths use:

```text
ShowPanel(reason)
Show popup if hidden
synchronize system-policy visibility
do not force activation
write sanitized shell lifecycle diagnostic
```

## Diagnostics

Sanitized records use category:

```text
shell.panel.lifecycle
```

Recorded fields:

```text
action
reason
previous visibility
current visibility
active state
focused-element type
popup bounds
working-area bounds
Topmost
ShowActivated
ShowInTaskbar
```

Diagnostics must never destabilize CoreHost lifecycle. Logging failures are swallowed.

## Manual acceptance gate

Automated tests cannot prove Windows shell visual behavior.

Owner replay must confirm:

```text
no tray notification-area darkening
no Microsoft Pinyin indicator jump
popup remains manually interactive
title-bar close still hides to tray
tray toggle works
global hotkey toggle works
Owner width and height persist
stacked overflow scrolling remains stable
```

## Fallback rule

Only if the low-risk policy fails manual replay may a follow-up checkpoint evaluate:

```text
stronger Win32 activation styles
custom chrome
bounded shadow policy
edge-safe placement inset
```

Do not mix those higher-risk changes into the first shell stabilization commit.
