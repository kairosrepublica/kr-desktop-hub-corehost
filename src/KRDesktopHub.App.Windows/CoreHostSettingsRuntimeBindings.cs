using KRDesktopHub.Core;
using KRDesktopHub.Platform.Windows;

namespace KRDesktopHub.App.Windows;

public static class CoreHostSettingsRuntimeBindings
{
    public static CoreHostPolicyOptions ToSystemPolicyOptions(
        CoreHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(
            settings);

        return new CoreHostPolicyOptions(
            BatteryAwareRefreshThrottling:
                settings.BatteryAwareRefreshThrottling,

            RefreshOnlyStaleWidgetsAfterResume:
                settings.RefreshStaleWidgetsAfterResume,

            ReplayMissedScheduledRunsAfterResume:
                settings.ReplayMissedScheduledRunsAfterResume,

            PauseNetworkHeavyWidgetsWhenLocked:
                settings.PauseNetworkHeavyWidgetsWhenLocked,

            PauseLowPriorityWidgetsOnBattery:
                settings.PauseLowPriorityWidgetsOnBattery,

            RefreshTimeWidgetsAfterTimeZoneChange:
                settings.RefreshTimeWidgetsAfterTimeZoneChange,

            RetryFailedTasksAfterNetworkRecovery:
                settings.RefreshFailedWidgetsAfterNetworkRecovery,

            StopVisualRefreshWhenPanelHidden:
                settings.SuspendVisualRefreshWhenPanelHidden,

            StopNetworkRequestsWhenWidgetInactive:
                settings.SuspendInactiveWidgetNetworkRequests,

            NetworkRecoveryDebounce:
                TimeSpan.FromSeconds(
                    settings.NetworkRecoveryDebounceSeconds),

            ResourceSampleInterval:
                TimeSpan.FromSeconds(
                    settings.ResourceSampleIntervalSeconds),

            IdleCpuWarningPercent:
                settings.IdleCpuWarningPercent,

            IdleWorkingSetWarningBytes:
                settings.IdleWorkingSetWarningMegabytes is null
                    ? null
                    : settings.IdleWorkingSetWarningMegabytes.Value
                        * 1024L
                        * 1024L);
    }

    public static NotificationGovernanceOptions ToNotificationGovernanceOptions(
        CoreHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(
            settings);

        return new NotificationGovernanceOptions(
            NotificationsEnabled:
                settings.NotificationsEnabled,

            SoundsEnabled:
                settings.NotificationSoundsEnabled,

            NormalNotificationLimitPerTenMinutes:
                settings.NormalNotificationLimitPerTenMinutes,

            MergeDuplicateNotifications:
                settings.MergeDuplicateNotifications,

            DuplicateNotificationMergeWindow:
                TimeSpan.FromSeconds(
                    settings.DuplicateNotificationMergeWindowSeconds),

            QuietHoursEnabled:
                settings.QuietHoursEnabled,

            QuietHoursStartLocal:
                ParseTime(
                    settings.QuietHoursStartLocal),

            QuietHoursEndLocal:
                ParseTime(
                    settings.QuietHoursEndLocal));
    }

    private static TimeOnly ParseTime(
        string value)
    {
        return TimeOnly.TryParse(
            value,
            out var parsed)
                ? parsed
                : throw new FormatException(
                    $"Invalid local time: {value}");
    }
}