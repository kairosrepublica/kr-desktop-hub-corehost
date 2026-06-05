# KR Desktop Hub System Policy and Notification Governance API â€” Stabilization Batch 8B2A

## Purpose

Bind CoreHost policy and notification-governance behavior to durable settings instead of fixed bootstrap defaults.

## CoreHost settings schema

Schema version:

```text
2
```

New settings:

```text
duplicate_notification_merge_window_seconds
quiet_hours_enabled
network_recovery_debounce_seconds
resource_sample_interval_seconds
idle_cpu_warning_percent
idle_working_set_warning_megabytes
```

Existing schema version `1` files are migrated safely to version `2`.

## System policy mapping

`CoreHostSettingsRuntimeBindings.ToSystemPolicyOptions()` maps durable settings into:

```text
CoreHostPolicyOptions
```

The mapping controls:

```text
battery-aware refresh throttling
resume refresh policy
missed-run replay policy
locked-session network restrictions
battery pause policy
time-zone refresh policy
network-recovery debounce
hidden-panel visual refresh suppression
inactive-Widget network suppression
resource-sampling interval
future idle-resource warning thresholds
```

`SystemPolicyCoordinator.UpdateOptions()` allows policy settings to be reloaded without restarting the CoreHost.

## Notification governance

`GovernedSystemNotificationService` wraps the current platform notification provider.

It applies:

```text
notifications-enabled gate
quiet hours for ordinary notifications
ordinary-notification rate limit
duplicate ordinary-notification suppression
important and urgent priority bypass
forced-delivery path for diagnostic and safety notifications
```

The service exposes whether sound is allowed by policy. The current tray-balloon backend does not guarantee fine-grained sound control. A future modern Windows notification provider may consume the sound policy more fully.

## Resource-sampling interval

`resource_sample_interval_seconds` is applied when the CoreHost process starts.

Changing this value through `Reload Settings` updates the durable setting. Restart the CoreHost to recreate the process resource monitor with the new interval.

## Boundary

Batch 8B2A does not implement Widget Runtime execution-policy enforcement. That is reserved for Batch 8B2B.

Batch 8B2A does not implement the final visual Settings interface. That is reserved for Batch 8D.