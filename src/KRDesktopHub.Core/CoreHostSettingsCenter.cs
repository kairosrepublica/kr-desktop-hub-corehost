using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace KRDesktopHub.Core;

public enum CoreHostSettingsApplyMode
{
    Immediate,
    RestartRequired,
    ReservedForFutureBinding
}

public sealed record CoreHostSettingDescriptor(
    string SectionId,
    string Key,
    string DisplayName,
    string Description,
    string RecommendedDefault,
    string RecommendationReason,
    CoreHostSettingsApplyMode ApplyMode);

public sealed class CoreHostSettingsCenterState
{
    public bool StartAfterWindowsLogin { get; set; } = true;
    public int StartupDelaySeconds { get; set; } = 10;
    public bool OpenPanelAfterLogin { get; set; }
    public bool RequestAdministratorPrivileges { get; set; }
    public bool CrashAutoRestart { get; set; } = true;
    public int MaximumCrashRestartAttempts { get; set; } = 5;

    public bool CloseButtonHidesToTray { get; set; } = true;
    public bool TrayIconEnabled { get; set; } = true;
    public bool PanelAlwaysOnTop { get; set; }
    public bool ClickOutsideToHide { get; set; }
    public bool OnePanelOnly { get; set; } = true;
    public bool RememberWindowPlacement { get; set; } = true;

    public string ShowHidePanelHotkey { get; set; } = "Ctrl+Alt+K";
    public string HidePanelHotkey { get; set; } = "";
    public string RefreshAllWidgetsHotkey { get; set; } = "";
    public string SwitchFocusMarketHotkey { get; set; } = "";

    public bool NotificationsEnabled { get; set; } = true;
    public bool NotificationCenterIntegration { get; set; } = true;
    public bool NotificationClickOpensCoreHost { get; set; } = true;
    public bool NotificationActionButtonsEnabled { get; set; } = true;
    public bool NotificationSoundEnabled { get; set; } = true;
    public string NotificationPriorityDefault { get; set; } = "normal";
    public int NotificationRateLimitPerMinute { get; set; } = 6;
    public bool NotificationDuplicateMerging { get; set; } = true;
    public string QuietHoursStart { get; set; } = "";
    public string QuietHoursEnd { get; set; } = "";

    public bool PauseVisualRefreshWhenPanelHidden { get; set; } = true;
    public bool PauseInactiveWidgetNetworkRequests { get; set; } = true;
    public bool ReduceRefreshFrequencyOnBattery { get; set; } = true;
    public string LockScreenPolicy { get; set; } = "suspend-noncritical";
    public string WakePolicy { get; set; } = "refresh-on-resume";
    public string TimeZoneChangePolicy { get; set; } = "refresh-on-change";
    public string NetworkRecoveryPolicy { get; set; } = "retry-with-backoff";
    public int WidgetRetryCount { get; set; } = 3;
    public int WidgetQuarantineThreshold { get; set; } = 3;
    public int MaximumConcurrentWidgetTasks { get; set; } = 4;
    public int DefaultWidgetTaskTimeoutSeconds { get; set; } = 20;

    public int LogRetentionDays { get; set; } = 14;
    public int ResourceSamplingIntervalSeconds { get; set; } = 60;
    public bool DiagnosticsRedactionEnabled { get; set; } = true;
    public string DiagnosticsExportPath { get; set; } = "";
    public int BackupRetentionCount { get; set; } = 5;
    public string PortableDataRootPath { get; set; } = "";
}

public sealed class CoreHostSettingsCenterDocument
{
    public int SchemaVersion { get; set; } = 1;

    public DateTimeOffset UpdatedAtUtc { get; set; } =
        DateTimeOffset.UtcNow;

    public CoreHostSettingsCenterState Settings { get; set; } =
        new();
}

public sealed class CoreHostSettingsValidationException
    : InvalidOperationException
{
    public CoreHostSettingsValidationException(
        IReadOnlyList<string> errors)
        : base(
            string.Join(
                Environment.NewLine,
                errors))
    {
        Errors =
            errors;
    }

    public IReadOnlyList<string> Errors { get; }
}

public static class CoreHostSettingsCenterCatalog
{
    public static IReadOnlyList<CoreHostSettingDescriptor> All { get; } =
        new[]
        {
            Describe("startup", "StartAfterWindowsLogin", "Start after Windows login", "Start the CoreHost after the user signs in.", "true", "The CoreHost is a desktop host and should be available without a manual launch.", CoreHostSettingsApplyMode.RestartRequired),
            Describe("startup", "StartupDelaySeconds", "Startup delay", "Delay launch after user login.", "10", "A short delay avoids competing with Windows login tasks.", CoreHostSettingsApplyMode.RestartRequired),
            Describe("startup", "OpenPanelAfterLogin", "Open panel after login", "Show the panel immediately after login.", "false", "The default should remain quiet and non-disruptive.", CoreHostSettingsApplyMode.RestartRequired),
            Describe("startup", "RequestAdministratorPrivileges", "Request administrator privileges", "Request elevated privileges at startup.", "false", "The CoreHost should run with standard user privileges unless a specific adapter requires elevation.", CoreHostSettingsApplyMode.RestartRequired),
            Describe("startup", "CrashAutoRestart", "Restart after crash", "Allow a limited automatic restart after an abnormal exit.", "true", "A desktop host should recover from transient faults.", CoreHostSettingsApplyMode.ReservedForFutureBinding),
            Describe("startup", "MaximumCrashRestartAttempts", "Maximum crash restarts", "Limit repeated crash restarts.", "5", "A finite limit avoids restart loops.", CoreHostSettingsApplyMode.ReservedForFutureBinding),

            Describe("panel-tray", "CloseButtonHidesToTray", "Close button hides to tray", "Hide the panel instead of exiting.", "true", "The tray lifecycle remains available without surprising shutdowns.", CoreHostSettingsApplyMode.Immediate),
            Describe("panel-tray", "TrayIconEnabled", "Tray icon enabled", "Keep the tray icon available.", "true", "The tray is the primary lifecycle control.", CoreHostSettingsApplyMode.RestartRequired),
            Describe("panel-tray", "PanelAlwaysOnTop", "Panel always on top", "Keep the panel above ordinary windows.", "false", "Always-on-top can interfere with other work and should remain opt-in.", CoreHostSettingsApplyMode.RestartRequired),
            Describe("panel-tray", "ClickOutsideToHide", "Click outside to hide", "Hide the panel when focus moves elsewhere.", "false", "Unexpected auto-hide can be disruptive.", CoreHostSettingsApplyMode.ReservedForFutureBinding),
            Describe("panel-tray", "OnePanelOnly", "One panel only", "Prevent duplicate panel windows.", "true", "A single panel avoids inconsistent state.", CoreHostSettingsApplyMode.Immediate),
            Describe("panel-tray", "RememberWindowPlacement", "Remember panel position and size", "Restore the last valid window placement.", "true", "Stable placement reduces friction on multi-monitor desktops.", CoreHostSettingsApplyMode.Immediate),

            Describe("hotkeys", "ShowHidePanelHotkey", "Show or hide panel hotkey", "Global shortcut for panel visibility.", "Ctrl+Alt+K", "A stable shortcut allows fast panel access.", CoreHostSettingsApplyMode.RestartRequired),
            Describe("hotkeys", "HidePanelHotkey", "Hide panel hotkey", "Optional dedicated shortcut for hiding the panel.", "", "Leave blank unless a separate hide shortcut is useful.", CoreHostSettingsApplyMode.RestartRequired),
            Describe("hotkeys", "RefreshAllWidgetsHotkey", "Refresh all Widgets hotkey", "Optional shortcut for a future Widget refresh command.", "", "Leave blank to reduce global-shortcut collisions.", CoreHostSettingsApplyMode.ReservedForFutureBinding),
            Describe("hotkeys", "SwitchFocusMarketHotkey", "Switch Focus Market hotkey", "Optional shortcut reserved for future financial-market Widgets.", "", "Leave blank until a Widget needs it.", CoreHostSettingsApplyMode.ReservedForFutureBinding),

            Describe("notifications", "NotificationsEnabled", "Notifications enabled", "Allow CoreHost-governed notifications.", "true", "Notifications are useful when governed by rate limits and quiet hours.", CoreHostSettingsApplyMode.Immediate),
            Describe("notifications", "NotificationCenterIntegration", "Notification Center integration", "Use modern Windows notification integration where available.", "true", "Notification Center provides a durable user-visible history.", CoreHostSettingsApplyMode.ReservedForFutureBinding),
            Describe("notifications", "NotificationClickOpensCoreHost", "Click notification to open CoreHost", "Activate the CoreHost when a notification is clicked.", "true", "Click-through behavior should lead to the relevant host context.", CoreHostSettingsApplyMode.ReservedForFutureBinding),
            Describe("notifications", "NotificationActionButtonsEnabled", "Notification action buttons", "Permit governed action buttons where supported.", "true", "Explicit action buttons reduce unnecessary navigation.", CoreHostSettingsApplyMode.ReservedForFutureBinding),
            Describe("notifications", "NotificationSoundEnabled", "Notification sound", "Allow notification sounds.", "true", "Sound remains useful but user-controllable.", CoreHostSettingsApplyMode.Immediate),
            Describe("notifications", "NotificationPriorityDefault", "Default notification priority", "Default priority class for governed notifications.", "normal", "Normal priority avoids alert fatigue.", CoreHostSettingsApplyMode.Immediate),
            Describe("notifications", "NotificationRateLimitPerMinute", "Notification rate limit per minute", "Limit notification bursts.", "6", "A modest cap prevents runaway Widget noise.", CoreHostSettingsApplyMode.Immediate),
            Describe("notifications", "NotificationDuplicateMerging", "Merge duplicate notifications", "Merge repeated equivalent notifications.", "true", "Deduplication reduces noise.", CoreHostSettingsApplyMode.Immediate),
            Describe("notifications", "QuietHoursStart", "Quiet-hours start", "Optional local time in HH:mm format.", "", "Leave blank unless quiet hours are required.", CoreHostSettingsApplyMode.Immediate),
            Describe("notifications", "QuietHoursEnd", "Quiet-hours end", "Optional local time in HH:mm format.", "", "Leave blank unless quiet hours are required.", CoreHostSettingsApplyMode.Immediate),

            Describe("runtime-resources", "PauseVisualRefreshWhenPanelHidden", "Pause visual refresh while hidden", "Stop unnecessary visual refresh work when the panel is hidden.", "true", "Hidden visual work wastes resources.", CoreHostSettingsApplyMode.Immediate),
            Describe("runtime-resources", "PauseInactiveWidgetNetworkRequests", "Pause inactive Widget network requests", "Prevent low-value polling by inactive Widgets.", "true", "Inactive Widgets should not waste network or battery resources.", CoreHostSettingsApplyMode.ReservedForFutureBinding),
            Describe("runtime-resources", "ReduceRefreshFrequencyOnBattery", "Reduce refresh frequency on battery", "Slow low-priority work when the computer is using battery power.", "true", "Battery-aware operation reduces resource cost.", CoreHostSettingsApplyMode.Immediate),
            Describe("runtime-resources", "LockScreenPolicy", "Lock-screen policy", "Policy applied when the Windows session locks.", "suspend-noncritical", "Noncritical work should pause during lock.", CoreHostSettingsApplyMode.Immediate),
            Describe("runtime-resources", "WakePolicy", "Wake policy", "Policy applied after resume.", "refresh-on-resume", "A refresh after resume restores current state.", CoreHostSettingsApplyMode.Immediate),
            Describe("runtime-resources", "TimeZoneChangePolicy", "Time-zone-change policy", "Policy applied after time-zone changes.", "refresh-on-change", "Time-sensitive Widgets must refresh after a time-zone change.", CoreHostSettingsApplyMode.Immediate),
            Describe("runtime-resources", "NetworkRecoveryPolicy", "Network-recovery policy", "Policy applied when network access returns.", "retry-with-backoff", "Backoff avoids synchronized request bursts.", CoreHostSettingsApplyMode.Immediate),
            Describe("runtime-resources", "WidgetRetryCount", "Widget retry count", "Maximum transient retry count.", "3", "A small retry count balances resilience and noise.", CoreHostSettingsApplyMode.Immediate),
            Describe("runtime-resources", "WidgetQuarantineThreshold", "Widget quarantine threshold", "Quarantine a repeatedly failing Widget.", "3", "Repeated faults should be isolated.", CoreHostSettingsApplyMode.Immediate),
            Describe("runtime-resources", "MaximumConcurrentWidgetTasks", "Maximum concurrent Widget tasks", "Limit concurrent Widget operations.", "4", "A conservative limit avoids resource spikes.", CoreHostSettingsApplyMode.Immediate),
            Describe("runtime-resources", "DefaultWidgetTaskTimeoutSeconds", "Default Widget task timeout", "Stop stalled Widget operations.", "20", "A finite timeout prevents hung tasks.", CoreHostSettingsApplyMode.Immediate),

            Describe("diagnostics-migration", "LogRetentionDays", "Log retention days", "Retain local diagnostic logs.", "14", "Two weeks is sufficient for ordinary troubleshooting.", CoreHostSettingsApplyMode.Immediate),
            Describe("diagnostics-migration", "ResourceSamplingIntervalSeconds", "Resource-sampling interval", "Sample resource usage periodically.", "60", "One-minute sampling balances evidence and overhead.", CoreHostSettingsApplyMode.Immediate),
            Describe("diagnostics-migration", "DiagnosticsRedactionEnabled", "Diagnostics redaction", "Redact sensitive fields from exported diagnostics.", "true", "Redaction should remain enabled by default.", CoreHostSettingsApplyMode.Immediate),
            Describe("diagnostics-migration", "DiagnosticsExportPath", "Diagnostics export path", "Optional destination for exported diagnostics.", "", "Leave blank to use the governed default.", CoreHostSettingsApplyMode.Immediate),
            Describe("diagnostics-migration", "BackupRetentionCount", "Backup retention count", "Retain a limited number of local migration backups.", "5", "A finite history is useful without unlimited storage growth.", CoreHostSettingsApplyMode.Immediate),
            Describe("diagnostics-migration", "PortableDataRootPath", "Portable data-root path", "Optional explicit data-root path.", "", "Leave blank to use the governed default resolver.", CoreHostSettingsApplyMode.RestartRequired)
        };

    private static CoreHostSettingDescriptor Describe(
        string sectionId,
        string key,
        string displayName,
        string description,
        string recommendedDefault,
        string recommendationReason,
        CoreHostSettingsApplyMode applyMode)
    {
        return new CoreHostSettingDescriptor(
            sectionId,
            key,
            displayName,
            description,
            recommendedDefault,
            recommendationReason,
            applyMode);
    }
}

public sealed class CoreHostSettingsCenterService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented =
                true
        };

    public CoreHostSettingsCenterService(
        string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataRoot);

        DataRoot =
            Path.GetFullPath(
                dataRoot);

        SettingsDirectory =
            Path.Combine(
                DataRoot,
                "config");

        SettingsPath =
            Path.Combine(
                SettingsDirectory,
                "corehost-settings-center.json");
    }

    public string DataRoot { get; }

    public string SettingsDirectory { get; }

    public string SettingsPath { get; }

    public CoreHostSettingsCenterDocument LoadOrCreate()
    {
        Directory.CreateDirectory(
            SettingsDirectory);

        if (!File.Exists(
            SettingsPath))
        {
            var created =
                new CoreHostSettingsCenterDocument();

            Save(
                created);

            return created;
        }

        var json =
            File.ReadAllText(
                SettingsPath);

        var loaded =
            JsonSerializer.Deserialize<
                CoreHostSettingsCenterDocument>(
                    json,
                    JsonOptions)
            ?? throw new InvalidOperationException(
                "Settings Center file is empty or invalid.");

        Validate(
            loaded.Settings);

        return loaded;
    }

    public void Save(
        CoreHostSettingsCenterDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        Validate(
            document.Settings);

        Directory.CreateDirectory(
            SettingsDirectory);

        document.SchemaVersion =
            1;

        document.UpdatedAtUtc =
            DateTimeOffset.UtcNow;

        var temporaryPath =
            SettingsPath
            + ".tmp";

        var backupPath =
            SettingsPath
            + ".bak";

        File.WriteAllText(
            temporaryPath,
            JsonSerializer.Serialize(
                document,
                JsonOptions));

        if (File.Exists(
            SettingsPath))
        {
            File.Copy(
                SettingsPath,
                backupPath,
                overwrite:
                    true);
        }

        File.Move(
            temporaryPath,
            SettingsPath,
            overwrite:
                true);
    }

    public static IReadOnlyList<string> Validate(
        CoreHostSettingsCenterState settings)
    {
        ArgumentNullException.ThrowIfNull(
            settings);

        var errors =
            new List<string>();

        RequireRange(
            errors,
            nameof(
                settings.StartupDelaySeconds),
            settings.StartupDelaySeconds,
            minimum:
                0,
            maximum:
                300);

        RequireRange(
            errors,
            nameof(
                settings.MaximumCrashRestartAttempts),
            settings.MaximumCrashRestartAttempts,
            minimum:
                0,
            maximum:
                20);

        RequireRange(
            errors,
            nameof(
                settings.NotificationRateLimitPerMinute),
            settings.NotificationRateLimitPerMinute,
            minimum:
                1,
            maximum:
                120);

        RequireRange(
            errors,
            nameof(
                settings.WidgetRetryCount),
            settings.WidgetRetryCount,
            minimum:
                0,
            maximum:
                20);

        RequireRange(
            errors,
            nameof(
                settings.WidgetQuarantineThreshold),
            settings.WidgetQuarantineThreshold,
            minimum:
                1,
            maximum:
                20);

        RequireRange(
            errors,
            nameof(
                settings.MaximumConcurrentWidgetTasks),
            settings.MaximumConcurrentWidgetTasks,
            minimum:
                1,
            maximum:
                64);

        RequireRange(
            errors,
            nameof(
                settings.DefaultWidgetTaskTimeoutSeconds),
            settings.DefaultWidgetTaskTimeoutSeconds,
            minimum:
                1,
            maximum:
                600);

        RequireRange(
            errors,
            nameof(
                settings.LogRetentionDays),
            settings.LogRetentionDays,
            minimum:
                1,
            maximum:
                365);

        RequireRange(
            errors,
            nameof(
                settings.ResourceSamplingIntervalSeconds),
            settings.ResourceSamplingIntervalSeconds,
            minimum:
                5,
            maximum:
                3600);

        RequireRange(
            errors,
            nameof(
                settings.BackupRetentionCount),
            settings.BackupRetentionCount,
            minimum:
                1,
            maximum:
                50);

        ValidateTime(
            errors,
            nameof(
                settings.QuietHoursStart),
            settings.QuietHoursStart);

        ValidateTime(
            errors,
            nameof(
                settings.QuietHoursEnd),
            settings.QuietHoursEnd);

        ValidateHotkeyCollisions(
            errors,
            settings);

        if (errors.Count
            > 0)
        {
            throw new CoreHostSettingsValidationException(
                errors);
        }

        return errors;
    }

    private static void RequireRange(
        ICollection<string> errors,
        string key,
        int value,
        int minimum,
        int maximum)
    {
        if (
            value
            < minimum
            || value
            > maximum
        )
        {
            errors.Add(
                $"{key} must be between {minimum} and {maximum}.");
        }
    }

    private static void ValidateTime(
        ICollection<string> errors,
        string key,
        string value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return;
        }

        if (!TimeOnly.TryParseExact(
            value,
            "HH:mm",
            out _))
        {
            errors.Add(
                $"{key} must be blank or use HH:mm.");
        }
    }

    private static void ValidateHotkeyCollisions(
        ICollection<string> errors,
        CoreHostSettingsCenterState settings)
    {
        var hotkeys =
            new Dictionary<
                string,
                string>(
                    StringComparer.OrdinalIgnoreCase);

        foreach (var pair in
            new[]
            {
                new KeyValuePair<string, string>(
                    nameof(
                        settings.ShowHidePanelHotkey),
                    settings.ShowHidePanelHotkey),
                new KeyValuePair<string, string>(
                    nameof(
                        settings.HidePanelHotkey),
                    settings.HidePanelHotkey),
                new KeyValuePair<string, string>(
                    nameof(
                        settings.RefreshAllWidgetsHotkey),
                    settings.RefreshAllWidgetsHotkey),
                new KeyValuePair<string, string>(
                    nameof(
                        settings.SwitchFocusMarketHotkey),
                    settings.SwitchFocusMarketHotkey)
            })
        {
            var normalized =
                pair.Value.Trim();

            if (string.IsNullOrWhiteSpace(
                normalized))
            {
                continue;
            }

            if (hotkeys.TryGetValue(
                normalized,
                out var existingKey))
            {
                errors.Add(
                    $"{pair.Key} collides with {existingKey}: {normalized}");
            }
            else
            {
                hotkeys[normalized] =
                    pair.Key;
            }
        }
    }
}