# KR Desktop Hub CoreHost v2.0.0 Manual Acceptance Checklist

## Purpose

Automated tests cannot prove real Windows taskbar, focus, input-method editor and shadow behavior.

Complete this checklist after the shell release-candidate checkpoint and portable ZIP generation.

## A. Clean extraction

```text
[ ] Extract KRDesktopHub_CoreHost_win-x64_portable_v2.0.0.zip into a new folder.
[ ] Run RUN_SELF_TEST.cmd.
[ ] Confirm the JSON marker reports PASS.
[ ] Run START_KR_DESKTOP_HUB.cmd.
[ ] Confirm one tray icon appears.
```

## B. Tray and popup lifecycle

```text
[ ] Double-click the tray icon repeatedly.
[ ] Confirm popup Show and Hide remain stable.
[ ] Press the configured global hotkey repeatedly.
[ ] Confirm popup Show and Hide remain stable.
[ ] Click the title-bar minimize button.
[ ] Confirm the popup disappears completely and only the tray icon remains.
[ ] Confirm the popup does not collapse toward the lower-left screen edge.
[ ] Click the title-bar close button.
[ ] Confirm the popup disappears and the process remains alive in the tray.
[ ] Open Settings Center and confirm the close-button policy can still be changed from hide-to-tray to exit.
[ ] Use tray Exit.
[ ] Confirm the process terminates.
```

## C. Taskbar notification-area visual replay

```text
[ ] Observe the tray notification area while opening and closing the popup at least 20 times.
[ ] Confirm the notification-area region does not darken.
[ ] Observe the popup lower edge near the taskbar.
[ ] Confirm no visually dirty shadow overlaps the tray region.
```

## D. Microsoft Pinyin replay

```text
[ ] Enable Microsoft Pinyin.
[ ] Keep the Chinese / English indicator visible.
[ ] Open and close the popup from the tray at least 20 times.
[ ] Open and close the popup from the global hotkey at least 20 times.
[ ] Confirm the Chinese / English indicator does not jump or flash.
[ ] Click Collapse once.
[ ] Confirm the first Collapse click does not disturb the indicator.
[ ] Click Expand once.
[ ] Confirm the first Expand click does not disturb the indicator.
[ ] Click elsewhere on the desktop after Collapse / Expand.
[ ] Confirm leaving the popup does not disturb the indicator.
[ ] Confirm the actual language mode does not change.
```

## E. Manual interaction

```text
[ ] Click inside the popup.
[ ] Confirm buttons remain interactive.
[ ] Open Settings Center.
[ ] Open Widget Management.
[ ] Collapse and Expand the sample Widget at least 50 times.
[ ] Confirm no Widget disappearance.
[ ] Confirm no degraded-snapshot warning.
```

## F. Owner geometry

```text
[ ] Widen the popup.
[ ] Adjust popup height.
[ ] Collapse and Expand the sample Widget.
[ ] Trigger manual Refresh.
[ ] Confirm outer popup width remains Owner-controlled.
[ ] Confirm outer popup height remains Owner-controlled.
[ ] Create stacked overflow.
[ ] Confirm host-level scrolling activates.
[ ] Restart CoreHost.
[ ] Confirm width and height persist.
```

## G. Shell diagnostics

```text
[ ] Open the CoreHost data-root logs folder.
[ ] Locate corehost-YYYYMMDD.jsonl.
[ ] Confirm records with category shell.panel.lifecycle exist.
[ ] Confirm records contain ShowActivated=False.
[ ] Confirm records contain ShowInTaskbar=False.
[ ] Confirm records contain noActivateExtendedStyle=True after the popup handle initializes.
[ ] Confirm no credentials or secrets appear.
```

## Acceptance decision

```text
[ ] PASS — authorize v2.0.0 GitHub release finalizer.
[ ] FAIL — record exact reproduction and attach sanitized shell diagnostics.
```
