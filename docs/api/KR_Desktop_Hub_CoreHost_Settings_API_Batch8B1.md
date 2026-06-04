# KR Desktop Hub CoreHost Settings API â€” Stabilization Batch 8B1

## Purpose

Create one durable CoreHost settings source of truth and bind the currently implemented shell behaviors to it.

## State files

```text
<DATA_ROOT>\config\corehost-settings.json
<DATA_ROOT>\state\hotkey-registration.json
```

## Manual edit workflow

Until the minimal Settings interface is implemented:

```text
Tray menu
Open Settings Folder
Edit corehost-settings.json
Tray menu
Reload Settings
```

## Runtime-bound settings in Batch 8B1

```text
login_startup_enabled
startup_delay_seconds
panel_hidden_after_login
close_button_hides_to_tray
always_on_top
toggle_panel_hotkey
toggle_panel_hotkey_fallbacks
notifications_enabled
```

## Schema-reserved settings awaiting Batch 8B2 policy binding

```text
notification_sounds_enabled
normal_notification_limit_per_ten_minutes
merge_duplicate_notifications
quiet_hours_start_local
quiet_hours_end_local
battery_aware_refresh_throttling
suspend_visual_refresh_when_panel_hidden
suspend_inactive_widget_network_requests
widget_retry_count
widget_quarantine_failure_threshold
widget_max_concurrent_tasks
widget_task_timeout_seconds
refresh_stale_widgets_after_resume
replay_missed_scheduled_runs_after_resume
pause_network_heavy_widgets_when_locked
pause_low_priority_widgets_on_battery
refresh_time_widgets_after_time_zone_change
refresh_failed_widgets_after_network_recovery
```

## Hotkey conflict behavior

The requested gesture is attempted first. If Windows rejects it, CoreHost attempts configured fallbacks in order. CoreHost records the requested gesture, attempted gestures, active gesture and last error under:

```text
<DATA_ROOT>\state\hotkey-registration.json
```

CoreHost does not overwrite another program's shortcut.

## Recommended-default reasons

`CoreHostSettingsCatalog.Recommendations` exposes the recommended default and the reason for important settings. The future Settings interface must display these explanations beside the editable controls.

## Boundary

Batch 8B1 does not implement the final Settings user interface and does not implement the Widget Package Manager.