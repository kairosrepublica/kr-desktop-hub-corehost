using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace KRDesktopHub.Platform.Windows;

public sealed record CoreHostSettings(
    int SchemaVersion,
    string Language,
    bool LoginStartupEnabled,
    int StartupDelaySeconds,
    bool PanelHiddenAfterLogin,
    bool CloseButtonHidesToTray,
    bool AlwaysOnTop,
    string TogglePanelHotkey,
    IReadOnlyList<string> TogglePanelHotkeyFallbacks,
    bool NotificationsEnabled,
    bool NotificationSoundsEnabled,
    int NormalNotificationLimitPerTenMinutes,
    bool MergeDuplicateNotifications,
    string QuietHoursStartLocal,
    string QuietHoursEndLocal,
    bool BatteryAwareRefreshThrottling,
    bool SuspendVisualRefreshWhenPanelHidden,
    bool SuspendInactiveWidgetNetworkRequests,
    int WidgetRetryCount,
    int WidgetQuarantineFailureThreshold,
    int WidgetMaxConcurrentTasks,
    int WidgetTaskTimeoutSeconds,
    bool RefreshStaleWidgetsAfterResume,
    bool ReplayMissedScheduledRunsAfterResume,
    bool PauseNetworkHeavyWidgetsWhenLocked,
    bool PauseLowPriorityWidgetsOnBattery,
    bool RefreshTimeWidgetsAfterTimeZoneChange,
    bool RefreshFailedWidgetsAfterNetworkRecovery,
    DateTimeOffset SavedAtUtc);

public sealed record CoreHostSettingRecommendation(
    string SettingName,
    string RecommendedValue,
    string Reason);

public sealed record HotkeyRegistrationRuntimeState(
    int SchemaVersion,
    string CommandId,
    string RequestedGesture,
    string? ActiveGesture,
    bool Registered,
    IReadOnlyList<string> AttemptedGestures,
    string? LastError,
    DateTimeOffset SavedAtUtc);

public static class CoreHostSettingsCatalog
{
    public const int CurrentSchemaVersion =
        1;

    public static CoreHostSettings Recommended =>
        new(
            SchemaVersion:
                CurrentSchemaVersion,

            Language:
                "en",

            LoginStartupEnabled:
                true,

            StartupDelaySeconds:
                10,

            PanelHiddenAfterLogin:
                true,

            CloseButtonHidesToTray:
                true,

            AlwaysOnTop:
                false,

            TogglePanelHotkey:
                "Ctrl+Alt+K",

            TogglePanelHotkeyFallbacks:
                new[]
                {
                    "Ctrl+Alt+H",
                    "Ctrl+Shift+K",
                    "Win+Alt+K"
                },

            NotificationsEnabled:
                true,

            NotificationSoundsEnabled:
                false,

            NormalNotificationLimitPerTenMinutes:
                6,

            MergeDuplicateNotifications:
                true,

            QuietHoursStartLocal:
                "23:00",

            QuietHoursEndLocal:
                "08:00",

            BatteryAwareRefreshThrottling:
                true,

            SuspendVisualRefreshWhenPanelHidden:
                true,

            SuspendInactiveWidgetNetworkRequests:
                true,

            WidgetRetryCount:
                5,

            WidgetQuarantineFailureThreshold:
                5,

            WidgetMaxConcurrentTasks:
                10,

            WidgetTaskTimeoutSeconds:
                30,

            RefreshStaleWidgetsAfterResume:
                true,

            ReplayMissedScheduledRunsAfterResume:
                false,

            PauseNetworkHeavyWidgetsWhenLocked:
                true,

            PauseLowPriorityWidgetsOnBattery:
                true,

            RefreshTimeWidgetsAfterTimeZoneChange:
                true,

            RefreshFailedWidgetsAfterNetworkRecovery:
                true,

            SavedAtUtc:
                DateTimeOffset.UtcNow);

    public static IReadOnlyList<CoreHostSettingRecommendation> Recommendations =>
        new[]
        {
            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.Language),
                "en",
                "Use an English-first interface while preserving the localization layer."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.LoginStartupEnabled),
                "true",
                "Launch after Windows login by default so the tray host is consistently available."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.StartupDelaySeconds),
                "10",
                "Avoid competing with the Windows login burst while keeping the host available promptly."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.PanelHiddenAfterLogin),
                "true",
                "Keep the CoreHost resident without interrupting the Owner after login."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.CloseButtonHidesToTray),
                "true",
                "Prevent accidental termination when the Owner closes the visible panel."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.AlwaysOnTop),
                "false",
                "Avoid forcing the panel above other work unless the Owner explicitly chooses it."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.TogglePanelHotkey),
                "Ctrl+Alt+K",
                "Provide a memorable low-friction default while preserving conflict detection and fallbacks."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.TogglePanelHotkeyFallbacks),
                "Ctrl+Alt+H; Ctrl+Shift+K; Win+Alt+K",
                "Offer deterministic alternatives when another application already owns the preferred shortcut."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.NotificationsEnabled),
                "true",
                "Keep important CoreHost state changes visible to the Owner."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.NotificationSoundsEnabled),
                "false",
                "Avoid unnecessary interruption. Urgent-notification sound policy can be refined later."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.NormalNotificationLimitPerTenMinutes),
                "6",
                "Limit ordinary interruptions while preserving a useful signal rate."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.MergeDuplicateNotifications),
                "true",
                "Prevent repetitive alerts from overwhelming the Owner."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.QuietHoursStartLocal),
                "23:00",
                "Suppress low-priority interruptions during the Owner's normal rest window."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.QuietHoursEndLocal),
                "08:00",
                "Resume ordinary notifications after the default rest window."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.BatteryAwareRefreshThrottling),
                "true",
                "Reduce background work on battery power."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.SuspendVisualRefreshWhenPanelHidden),
                "true",
                "Stop meaningless visual work when the panel is not visible."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.SuspendInactiveWidgetNetworkRequests),
                "true",
                "Prevent unnecessary background network traffic from inactive Widgets."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.WidgetRetryCount),
                "5",
                "Retry transient failures without allowing endless retry loops."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.WidgetQuarantineFailureThreshold),
                "5",
                "Isolate repeatedly failing Widgets before they degrade the host."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.WidgetMaxConcurrentTasks),
                "10",
                "Bound task fan-out while leaving room for multiple lightweight Widgets."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.WidgetTaskTimeoutSeconds),
                "30",
                "Terminate stalled Widget work before it accumulates."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.RefreshStaleWidgetsAfterResume),
                "true",
                "Refresh data that became stale while the computer slept."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.ReplayMissedScheduledRunsAfterResume),
                "false",
                "Avoid a burst of stale tasks after a long sleep interval."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.PauseNetworkHeavyWidgetsWhenLocked),
                "true",
                "Reduce unnecessary background traffic while the session is locked."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.PauseLowPriorityWidgetsOnBattery),
                "true",
                "Preserve battery life by pausing work that is not time-sensitive."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.RefreshTimeWidgetsAfterTimeZoneChange),
                "true",
                "Correct time-sensitive displays immediately after a time-zone change."),

            new CoreHostSettingRecommendation(
                nameof(
                    CoreHostSettings.RefreshFailedWidgetsAfterNetworkRecovery),
                "true",
                "Retry stale failed work after connectivity returns while preserving later rate limits.")
        };

}

public static class CoreHostHotkeyPolicy
{
    public static string NormalizeGesture(
        string? gesture,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(
            gesture))
        {
            return fallback;
        }

        var parts =
            gesture
                .Split(
                    '+',
                    StringSplitOptions.RemoveEmptyEntries
                    | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            return fallback;
        }

        return string.Join(
            '+',
            parts);
    }

    public static IReadOnlyList<string> GetCandidateGestures(
        CoreHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(
            settings);

        return new[]
            {
                settings.TogglePanelHotkey
            }
            .Concat(
                settings.TogglePanelHotkeyFallbacks
                ?? Array.Empty<string>())
            .Where(
                gesture =>
                    !string.IsNullOrWhiteSpace(
                        gesture))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public static class CoreHostSettingsValidator
{
    public static CoreHostSettings Normalize(
        CoreHostSettings? candidate)
    {
        var defaults =
            CoreHostSettingsCatalog.Recommended;

        if (
            candidate is null
            || candidate.SchemaVersion !=
                CoreHostSettingsCatalog.CurrentSchemaVersion
        )
        {
            return defaults;
        }

        var primaryGesture =
            CoreHostHotkeyPolicy.NormalizeGesture(
                candidate.TogglePanelHotkey,
                defaults.TogglePanelHotkey);

        var fallbackGestures =
            (
                candidate.TogglePanelHotkeyFallbacks
                ?? Array.Empty<string>()
            )
                .Concat(
                    defaults.TogglePanelHotkeyFallbacks)
                .Select(
                    gesture =>
                        CoreHostHotkeyPolicy.NormalizeGesture(
                            gesture,
                            string.Empty))
                .Where(
                    gesture =>
                        !string.IsNullOrWhiteSpace(
                            gesture)
                        && !string.Equals(
                            gesture,
                            primaryGesture,
                            StringComparison.OrdinalIgnoreCase))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return candidate with
        {
            SchemaVersion =
                CoreHostSettingsCatalog.CurrentSchemaVersion,

            Language =
                string.IsNullOrWhiteSpace(
                    candidate.Language)
                    ? defaults.Language
                    : candidate.Language.Trim(),

            StartupDelaySeconds =
                Math.Clamp(
                    candidate.StartupDelaySeconds,
                    0,
                    300),

            TogglePanelHotkey =
                primaryGesture,

            TogglePanelHotkeyFallbacks =
                fallbackGestures,

            NormalNotificationLimitPerTenMinutes =
                Math.Clamp(
                    candidate.NormalNotificationLimitPerTenMinutes,
                    0,
                    120),

            WidgetRetryCount =
                Math.Clamp(
                    candidate.WidgetRetryCount,
                    0,
                    20),

            WidgetQuarantineFailureThreshold =
                Math.Clamp(
                    candidate.WidgetQuarantineFailureThreshold,
                    1,
                    100),

            WidgetMaxConcurrentTasks =
                Math.Clamp(
                    candidate.WidgetMaxConcurrentTasks,
                    1,
                    100),

            WidgetTaskTimeoutSeconds =
                Math.Clamp(
                    candidate.WidgetTaskTimeoutSeconds,
                    1,
                    3600),

            QuietHoursStartLocal =
                NormalizeLocalTime(
                    candidate.QuietHoursStartLocal,
                    defaults.QuietHoursStartLocal),

            QuietHoursEndLocal =
                NormalizeLocalTime(
                    candidate.QuietHoursEndLocal,
                    defaults.QuietHoursEndLocal),

            SavedAtUtc =
                DateTimeOffset.UtcNow
        };
    }

    private static string NormalizeLocalTime(
        string? candidate,
        string fallback)
    {
        return TimeOnly.TryParse(
            candidate,
            out var parsed)
                ? parsed.ToString(
                    "HH:mm")
                : fallback;
    }
}

public sealed class JsonCoreHostSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.SnakeCaseLower,

            PropertyNameCaseInsensitive =
                true,

            WriteIndented =
                true
        };

    private readonly object _saveGate =
        new();

    public JsonCoreHostSettingsStore(
        string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataRoot);

        SettingsDirectory =
            Path.Combine(
                dataRoot,
                "config");

        SettingsFilePath =
            Path.Combine(
                SettingsDirectory,
                "corehost-settings.json");
    }

    public string SettingsDirectory { get; }

    public string SettingsFilePath { get; }

    public CoreHostSettings LoadOrCreateRecommended()
    {
        var settings =
            CoreHostSettingsValidator.Normalize(
                TryLoad());

        Save(
            settings);

        return settings;
    }

    public CoreHostSettings Reload()
    {
        var settings =
            CoreHostSettingsValidator.Normalize(
                TryLoad());

        Save(
            settings);

        return settings;
    }

    public void Save(
        CoreHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(
            settings);

        SaveJsonAtomically(
            SettingsFilePath,
            CoreHostSettingsValidator.Normalize(
                settings));
    }

    private CoreHostSettings? TryLoad()
    {
        try
        {
            if (!File.Exists(
                SettingsFilePath))
            {
                return null;
            }

            return JsonSerializer.Deserialize<CoreHostSettings>(
                File.ReadAllText(
                    SettingsFilePath),

                JsonOptions);
        }
        catch (
            Exception exception)
            when (
                exception is IOException
                or UnauthorizedAccessException
                or JsonException)
        {
            return null;
        }
    }

    private void SaveJsonAtomically<T>(
        string path,
        T value)
    {
        lock (_saveGate)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    path)
                ?? throw new InvalidOperationException(
                    "Unable to resolve settings directory."));

            var temporaryPath =
                path
                + "."
                + Guid
                    .NewGuid()
                    .ToString(
                        "N")
                + ".tmp";

            try
            {
                File.WriteAllText(
                    temporaryPath,
                    JsonSerializer.Serialize(
                        value,
                        JsonOptions));

                File.Move(
                    temporaryPath,
                    path,
                    overwrite:
                        true);
            }
            finally
            {
                if (File.Exists(
                    temporaryPath))
                {
                    File.Delete(
                        temporaryPath);
                }
            }
        }
    }
}

public sealed class JsonHotkeyRegistrationRuntimeStateStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.SnakeCaseLower,

            WriteIndented =
                true
        };

    private readonly object _saveGate =
        new();

    public JsonHotkeyRegistrationRuntimeStateStore(
        string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataRoot);

        StateFilePath =
            Path.Combine(
                dataRoot,
                "state",
                "hotkey-registration.json");
    }

    public string StateFilePath { get; }

    public void Save(
        HotkeyRegistrationRuntimeState state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        lock (_saveGate)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    StateFilePath)
                ?? throw new InvalidOperationException(
                    "Unable to resolve state directory."));

            var temporaryPath =
                StateFilePath
                + "."
                + Guid
                    .NewGuid()
                    .ToString(
                        "N")
                + ".tmp";

            try
            {
                File.WriteAllText(
                    temporaryPath,
                    JsonSerializer.Serialize(
                        state,
                        JsonOptions));

                File.Move(
                    temporaryPath,
                    StateFilePath,
                    overwrite:
                        true);
            }
            finally
            {
                if (File.Exists(
                    temporaryPath))
                {
                    File.Delete(
                        temporaryPath);
                }
            }
        }
    }
}