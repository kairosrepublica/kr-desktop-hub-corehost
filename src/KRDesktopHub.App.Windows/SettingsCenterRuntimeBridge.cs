using System;
using KRDesktopHub.Core;
using KRDesktopHub.Platform.Windows;

namespace KRDesktopHub.App.Windows;

public sealed class SettingsCenterRuntimeBridge
{
    private readonly CoreHostSettingsCenterService _settingsCenterStore;

    private readonly JsonCoreHostSettingsStore _runtimeStore;

    public SettingsCenterRuntimeBridge(
        string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataRoot);

        _settingsCenterStore =
            new CoreHostSettingsCenterService(
                dataRoot);

        _runtimeStore =
            new JsonCoreHostSettingsStore(
                dataRoot);
    }

    public string SettingsDirectory =>
        _settingsCenterStore.SettingsDirectory;

    public CoreHostSettingsCenterDocument LoadOrCreate()
    {
        var document =
            _settingsCenterStore.LoadOrCreate();

        var runtime =
            _runtimeStore.LoadOrCreateRecommended();

        OverlayRuntimeBackedValues(
            document.Settings,
            runtime);

        _settingsCenterStore.Save(
            document);

        return document;
    }

    public void Save(
        CoreHostSettingsCenterDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        _settingsCenterStore.Save(
            document);

        var runtime =
            _runtimeStore.Reload();

        var updatedRuntime =
            runtime with
            {
                LoginStartupEnabled =
                    document.Settings.StartAfterWindowsLogin,

                StartupDelaySeconds =
                    document.Settings.StartupDelaySeconds,

                PanelHiddenAfterLogin =
                    !document.Settings.OpenPanelAfterLogin,

                CloseButtonHidesToTray =
                    document.Settings.CloseButtonHidesToTray,

                AlwaysOnTop =
                    document.Settings.PanelAlwaysOnTop,

                TogglePanelHotkey =
                    document.Settings.ShowHidePanelHotkey,

                NotificationsEnabled =
                    document.Settings.NotificationsEnabled,

                NotificationSoundsEnabled =
                    document.Settings.NotificationSoundEnabled,

                MergeDuplicateNotifications =
                    document.Settings.NotificationDuplicateMerging,

                QuietHoursStartLocal =
                    NormalizeOptionalTime(
                        document.Settings.QuietHoursStart,
                        runtime.QuietHoursStartLocal),

                QuietHoursEndLocal =
                    NormalizeOptionalTime(
                        document.Settings.QuietHoursEnd,
                        runtime.QuietHoursEndLocal),

                BatteryAwareRefreshThrottling =
                    document.Settings.ReduceRefreshFrequencyOnBattery,

                SuspendVisualRefreshWhenPanelHidden =
                    document.Settings.PauseVisualRefreshWhenPanelHidden,

                SuspendInactiveWidgetNetworkRequests =
                    document.Settings.PauseInactiveWidgetNetworkRequests,

                WidgetRetryCount =
                    document.Settings.WidgetRetryCount,

                WidgetQuarantineFailureThreshold =
                    document.Settings.WidgetQuarantineThreshold,

                WidgetMaxConcurrentTasks =
                    document.Settings.MaximumConcurrentWidgetTasks,

                WidgetTaskTimeoutSeconds =
                    document.Settings.DefaultWidgetTaskTimeoutSeconds,

                SavedAtUtc =
                    DateTimeOffset.UtcNow
            };

        _runtimeStore.Save(
            updatedRuntime);

        var normalizedRuntime =
            _runtimeStore.Reload();

        OverlayRuntimeBackedValues(
            document.Settings,
            normalizedRuntime);

        _settingsCenterStore.Save(
            document);
    }

    private static void OverlayRuntimeBackedValues(
        CoreHostSettingsCenterState target,
        CoreHostSettings runtime)
    {
        target.StartAfterWindowsLogin =
            runtime.LoginStartupEnabled;

        target.StartupDelaySeconds =
            runtime.StartupDelaySeconds;

        target.OpenPanelAfterLogin =
            !runtime.PanelHiddenAfterLogin;

        target.CloseButtonHidesToTray =
            runtime.CloseButtonHidesToTray;

        target.PanelAlwaysOnTop =
            runtime.AlwaysOnTop;

        target.ShowHidePanelHotkey =
            runtime.TogglePanelHotkey;

        target.NotificationsEnabled =
            runtime.NotificationsEnabled;

        target.NotificationSoundEnabled =
            runtime.NotificationSoundsEnabled;

        target.NotificationDuplicateMerging =
            runtime.MergeDuplicateNotifications;

        target.QuietHoursStart =
            runtime.QuietHoursStartLocal;

        target.QuietHoursEnd =
            runtime.QuietHoursEndLocal;

        target.ReduceRefreshFrequencyOnBattery =
            runtime.BatteryAwareRefreshThrottling;

        target.PauseVisualRefreshWhenPanelHidden =
            runtime.SuspendVisualRefreshWhenPanelHidden;

        target.PauseInactiveWidgetNetworkRequests =
            runtime.SuspendInactiveWidgetNetworkRequests;

        target.WidgetRetryCount =
            runtime.WidgetRetryCount;

        target.WidgetQuarantineThreshold =
            runtime.WidgetQuarantineFailureThreshold;

        target.MaximumConcurrentWidgetTasks =
            runtime.WidgetMaxConcurrentTasks;

        target.DefaultWidgetTaskTimeoutSeconds =
            runtime.WidgetTaskTimeoutSeconds;
    }

    private static string NormalizeOptionalTime(
        string value,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(
            value)
                ? fallback
                : value;
    }
}