# KR Desktop Hub CoreHost Windows Shell Stabilization v1.1

## Scope

This checkpoint closes the remaining Windows tray-popup lifecycle defects before CoreHost `v2.0.0` release.

## Owner replay findings after the low-risk shell checkpoint

The first shell checkpoint correctly removed forced activation during ordinary popup Show and stopped repeated Microsoft Pinyin jumps during continuous Collapse / Expand clicks.

Owner replay still proved two residual defects:

```text
clicking the title-bar minimize button collapsed the popup toward the lower-left screen edge instead of hiding it to the tray

the first Collapse / Expand click still disturbed the Microsoft Pinyin Chinese / English indicator

clicking elsewhere after a Collapse / Expand interaction still disturbed the Microsoft Pinyin indicator
```

## Root-cause assessment

`ShowActivated = false` prevents activation during ordinary programmatic Show.

It does not prevent the popup from becoming active when the Owner manually clicks inside the Windows Presentation Foundation window.

The remaining input-method-editor disturbance is therefore consistent with focus entering and leaving the top-level popup.

The minimize defect is separate: the ordinary Windows minimize system command was still allowed to change the top-level window state.

## Final tray-popup policy

The CoreHost popup now uses:

```text
ShowActivated = false
ShowInTaskbar = false
ordinary Show does not force Activate()
WS_EX_NOACTIVATE on the main tray-popup HWND
intercept WM_SYSCOMMAND / SC_MINIMIZE
convert minimize into HidePanel("title-bar-minimize-to-tray")
standard Collapse / Expand button:
    Focusable = false
    IsTabStop = false
```

`HWND` means the native Windows window handle.

`WM_SYSCOMMAND` is the Windows system-command message.

`SC_MINIMIZE` is the native minimize command.

`WS_EX_NOACTIVATE` is a Windows extended style that prevents the tray popup from becoming the foreground active window when clicked.

## Responsibility boundary

Apply the stronger no-activate policy only to:

```text
MainWindow tray popup
```

Do not apply it to:

```text
Settings Center
Widget Management
governed floating dialogs
future editable Widget dialogs
```

Those separate windows may require normal keyboard focus and input-method-editor behavior.

## Close and minimize lifecycle

Default title-bar close behavior:

```text
hide to tray
```

This is already governed by:

```text
CloseButtonHidesToTray = true
```

The setting remains available so the Owner can choose:

```text
close button hides to tray
or
close button exits the application
```

Title-bar minimize behavior is fixed:

```text
hide to tray
```

It does not exit the process and does not persist a minimized popup state.

## Diagnostics

Sanitized `shell.panel.lifecycle` records now include:

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
noActivateExtendedStyle
```

## Widget developer boundary

The main tray popup is a non-activating information surface.

Production Widgets that require text entry, keyboard-intensive interaction or input-method-editor composition must request a governed dialog rather than placing editable controls directly inside the non-activating tray popup.

## Manual acceptance gate

Owner replay must confirm:

```text
title-bar minimize hides the popup and leaves only the tray icon
title-bar close hides to tray by default
the close-setting exit alternative remains available
no Microsoft Pinyin jump on first Collapse / Expand click
no Microsoft Pinyin jump after clicking elsewhere
no tray notification-area darkening
popup remains mouse-interactive
tray toggle works
global hotkey toggle works
Owner geometry persists
host-level scrolling remains stable
```
