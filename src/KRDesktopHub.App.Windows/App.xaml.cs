using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Windows;
using KRDesktopHub.Contracts;
using KRDesktopHub.Core;
using KRDesktopHub.Platform.Abstractions;
using KRDesktopHub.Platform.Windows;

namespace KRDesktopHub.App.Windows;

public partial class App : Application
{
    private const string MutexName =
        @"Local\KRDesktopHub.CoreHost";

    private Mutex? _mutex;
    private bool _ownsMutex;
    private MainWindow? _panel;
    private WindowsTrayService? _tray;
    private WindowsGlobalHotkeyService? _hotkeys;
    private WindowsStartupRegistrationService? _startup;
    private WindowsTrayBalloonNotificationService? _notifications;
    private GovernedSystemNotificationService? _governedNotifications;
    private CoreHostPolicyOptions _policyOptions = CoreHostPolicyOptions.Recommended;
    private WindowsPowerStateService? _power;
    private WindowsNetworkStateService? _network;
    private WindowsSessionStateService? _session;
    private WindowsTimeZoneChangeService? _timeZone;
    private WindowsProcessResourceMonitorService? _resources;
    private SystemPolicyCoordinator? _systemPolicies;
    private WindowsWindowPlacementService? _windowPlacement;
    private JsonCoreHostSettingsStore? _settingsStore;
    private JsonHotkeyRegistrationRuntimeStateStore? _hotkeyRuntimeStateStore;
    private CoreHostSettings _settings = CoreHostSettingsCatalog.Recommended;

    protected override async void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var options =
                LaunchOptions.Parse(
                    e.Args);

            if (!string.IsNullOrWhiteSpace(
                options.SelfTestMarkerPath))
            {
                await WriteSelfTestMarkerAsync(
                    options.SelfTestMarkerPath);

                Shutdown(
                    exitCode:
                        0);

                return;
            }

            _mutex =
                new Mutex(
                    initiallyOwned:
                        true,

                    name:
                        MutexName,

                    createdNew:
                        out _ownsMutex);

            if (!_ownsMutex)
            {
                MessageBox.Show(
                    "KR Desktop Hub is already running.",
                    "KR Desktop Hub",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Shutdown();
                return;
            }

            if (options.StartupDelay >
                TimeSpan.Zero)
            {
                await Task.Delay(
                    options.StartupDelay);
            }

            _panel =
                new MainWindow();

            _windowPlacement =
                new WindowsWindowPlacementService(
                    CoreHostDataRootResolver
                        .ResolveDefaultDataRoot());

            _windowPlacement.Attach(
                _panel);

            var dataRoot =
                CoreHostDataRootResolver
                    .ResolveDefaultDataRoot();

            _settingsStore =
                new JsonCoreHostSettingsStore(
                    dataRoot);

            _hotkeyRuntimeStateStore =
                new JsonHotkeyRegistrationRuntimeStateStore(
                    dataRoot);

            _settings =
                _settingsStore.LoadOrCreateRecommended();

            ApplyPanelSettings();

            _panel.CloseExitRequested +=
                (_, _) =>
                    ExitApplication();

            _tray =
                new WindowsTrayService();

            await _tray.InitializeAsync(
                CancellationToken.None);

            await _tray.SetStatusAsync(
                new TrayStatus(
                    "KR Desktop Hub ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â Ready"),

                CancellationToken.None);

            _notifications =
                new WindowsTrayBalloonNotificationService(
                    _tray);

            _policyOptions =
                CoreHostSettingsRuntimeBindings
                    .ToSystemPolicyOptions(
                        _settings);

            _governedNotifications =
                new GovernedSystemNotificationService(
                    _notifications,
                    CoreHostSettingsRuntimeBindings
                        .ToNotificationGovernanceOptions(
                            _settings));

            _power =
                new WindowsPowerStateService();

            _network =
                new WindowsNetworkStateService();

            _session =
                new WindowsSessionStateService();

            _timeZone =
                new WindowsTimeZoneChangeService();

            _resources =
                new WindowsProcessResourceMonitorService(
                    _policyOptions
                        .ResourceSampleInterval);

            _systemPolicies =
                new SystemPolicyCoordinator(
                    new InMemoryEventBus(),
                    _power,
                    _network,
                    _session,
                    _timeZone,
                    _resources,
                    new SystemPolicyEvaluator(
                        _policyOptions));

            await _resources.StartAsync(
                CancellationToken.None);

            _startup =
                new WindowsStartupRegistrationService(
                    Environment.ProcessPath
                    ?? throw new InvalidOperationException(
                        "Unable to determine executable path."));

            await ApplyStartupSettingsAsync();

            _hotkeys =
                new WindowsGlobalHotkeyService();

            _hotkeys.Attach(
                _panel);

            _hotkeys.CommandInvoked +=
                (_, commandId) =>
                {
                    if (string.Equals(
                        commandId,
                        "panel.toggle",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        TogglePanel();
                    }
                };

            await RegisterConfiguredHotkeyAsync();

            _tray.ToggleRequested +=
                (_, _) =>
                    TogglePanel();

            _tray.ExitRequested +=
                (_, _) =>
                    ExitApplication();

            _tray.TestNotificationRequested +=
                async (_, _) =>
                    await PublishNotificationAsync(
                        new SystemNotification(
                            "notification.test",
                            "KR Desktop Hub",
                            "System-tray notification is working.",
                            NotificationPriority.Informational,
                            Array.Empty<NotificationAction>()),

                        force:
                            true);

            _tray.StartupToggleRequested +=
                async (_, _) =>
                {
                    _settings =
                        _settings with
                        {
                            LoginStartupEnabled =
                                !_settings.LoginStartupEnabled,

                            SavedAtUtc =
                                DateTimeOffset.UtcNow
                        };

                    _settingsStore?.Save(
                        _settings);

                    await ApplyStartupSettingsAsync();

                    await PublishNotificationAsync(
                        new SystemNotification(
                            "startup.registration.changed",
                            "KR Desktop Hub",
                            _settings.LoginStartupEnabled
                                ? "Launch at login is enabled."
                                : "Launch at login is disabled.",
                            NotificationPriority.Informational,
                            Array.Empty<NotificationAction>()));
                };

            _tray.SettingsReloadRequested +=
                async (_, _) =>
                    await ReloadSettingsAsync();

            _tray.SettingsFolderRequested +=
                (_, _) =>
                    OpenSettingsFolder();

            if (
                options.ShowPanel
                || !_settings.PanelHiddenAfterLogin
            )
            {
                ShowPanel();
            }
            else
            {
                _panel.Hide();

                _systemPolicies.SetPanelVisibility(
                    isVisible:
                        false);
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.ToString(),
                "KR Desktop Hub startup failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }

    protected override void OnExit(
        ExitEventArgs e)
    {
        _windowPlacement?.SaveNow();
        _windowPlacement?.Dispose();

        _systemPolicies?.Dispose();

        if (_resources is not null)
        {
            _resources.DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();
        }

        _timeZone?.Dispose();
        _session?.Dispose();
        _network?.Dispose();
        _power?.Dispose();
        _hotkeys?.Dispose();

        if (_tray is not null)
        {
            _tray.DisposeAsync()
                .GetAwaiter()
                .GetResult();
        }

        if (_ownsMutex)
        {
            _mutex?.ReleaseMutex();
        }

        _mutex?.Dispose();

        base.OnExit(
            e);
    }

    private void ApplyPanelSettings()
    {
        if (_panel is null)
        {
            return;
        }

        _panel.Topmost =
            _settings.AlwaysOnTop;

        _panel.CloseButtonHidesToTray =
            _settings.CloseButtonHidesToTray;
    }

    private void ApplyPolicySettings()
    {
        _policyOptions =
            CoreHostSettingsRuntimeBindings
                .ToSystemPolicyOptions(
                    _settings);

        _systemPolicies?.UpdateOptions(
            _policyOptions);

        _governedNotifications?.UpdateOptions(
            CoreHostSettingsRuntimeBindings
                .ToNotificationGovernanceOptions(
                    _settings));
    }
    private async Task ApplyStartupSettingsAsync()
    {
        if (_startup is null)
        {
            return;
        }

        await _startup.SetAsync(
            new StartupRegistration(
                Enabled:
                    _settings.LoginStartupEnabled,

                Delay:
                    TimeSpan.FromSeconds(
                        _settings.StartupDelaySeconds)),

            CancellationToken.None);
    }

    private async Task RegisterConfiguredHotkeyAsync()
    {
        if (_hotkeys is null)
        {
            return;
        }

        await _hotkeys.UnregisterAllAsync(
            CancellationToken.None);

        var candidates =
            CoreHostHotkeyPolicy.GetCandidateGestures(
                _settings);

        var attempted =
            new List<string>();

        Exception? lastError =
            null;

        foreach (var gesture in candidates)
        {
            attempted.Add(
                gesture);

            try
            {
                await _hotkeys.RegisterAsync(
                    new HotkeyRegistration(
                        "panel.toggle",
                        gesture),

                    CancellationToken.None);

                _hotkeyRuntimeStateStore?.Save(
                    new HotkeyRegistrationRuntimeState(
                        SchemaVersion:
                            1,

                        CommandId:
                            "panel.toggle",

                        RequestedGesture:
                            _settings.TogglePanelHotkey,

                        ActiveGesture:
                            gesture,

                        Registered:
                            true,

                        AttemptedGestures:
                            attempted,

                        LastError:
                            null,

                        SavedAtUtc:
                            DateTimeOffset.UtcNow));

                if (!string.Equals(
                    gesture,
                    _settings.TogglePanelHotkey,
                    StringComparison.OrdinalIgnoreCase))
                {
                    await PublishNotificationAsync(
                        new SystemNotification(
                            "hotkey.registration.fallback",
                            "KR Desktop Hub",
                            $"Requested hotkey {_settings.TogglePanelHotkey} was unavailable. Active fallback: {gesture}.",
                            NotificationPriority.Important,
                            Array.Empty<NotificationAction>()),

                        force:
                            true);
                }

                return;
            }
            catch (Exception exception)
            {
                lastError =
                    exception;
            }
        }

        _hotkeyRuntimeStateStore?.Save(
            new HotkeyRegistrationRuntimeState(
                SchemaVersion:
                    1,

                CommandId:
                    "panel.toggle",

                RequestedGesture:
                    _settings.TogglePanelHotkey,

                ActiveGesture:
                    null,

                Registered:
                    false,

                AttemptedGestures:
                    attempted,

                LastError:
                    lastError?.Message,

                SavedAtUtc:
                    DateTimeOffset.UtcNow));

        await PublishNotificationAsync(
            new SystemNotification(
                "hotkey.registration.failed",
                "KR Desktop Hub",
                $"Global hotkey registration failed. Attempted: {string.Join(", ", attempted)}. Last error: {lastError?.Message}",
                NotificationPriority.Important,
                Array.Empty<NotificationAction>()),

            force:
                true);
    }

    private async Task ReloadSettingsAsync()
    {
        if (_settingsStore is null)
        {
            return;
        }

        try
        {
            _settings =
                _settingsStore.Reload();

            ApplyPanelSettings();
            ApplyPolicySettings();

            await ApplyStartupSettingsAsync();
            await RegisterConfiguredHotkeyAsync();

            await PublishNotificationAsync(
                new SystemNotification(
                    "settings.reload.succeeded",
                    "KR Desktop Hub",
                    "CoreHost settings reloaded successfully.",
                    NotificationPriority.Informational,
                    Array.Empty<NotificationAction>()));
        }
        catch (Exception exception)
        {
            await PublishNotificationAsync(
                new SystemNotification(
                    "settings.reload.failed",
                    "KR Desktop Hub",
                    $"CoreHost settings reload failed: {exception.Message}",
                    NotificationPriority.Important,
                    Array.Empty<NotificationAction>()));
        }
    }

    private void OpenSettingsFolder()
    {
        if (_settingsStore is null)
        {
            return;
        }

        Directory.CreateDirectory(
            _settingsStore.SettingsDirectory);

        Process.Start(
            new ProcessStartInfo
            {
                FileName =
                    _settingsStore.SettingsDirectory,

                UseShellExecute =
                    true
            });
    }

    private async Task PublishNotificationAsync(
        SystemNotification notification,
        bool force =
            false)
    {
        if (_governedNotifications is null)
        {
            return;
        }

        _ =
            await _governedNotifications.PublishAsync(
                notification,
                force,
                CancellationToken.None);
    }
    private static async Task WriteSelfTestMarkerAsync(
        string markerPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            markerPath);

        var parent =
            Path.GetDirectoryName(
                markerPath);

        if (!string.IsNullOrWhiteSpace(
            parent))
        {
            Directory.CreateDirectory(
                parent);
        }

        var marker =
            new SelfTestMarker(
                Status:
                    "PASS",

                TimestampUtc:
                    DateTimeOffset.UtcNow,

                ExecutablePath:
                    Environment.ProcessPath
                    ?? string.Empty,

                OperatingSystem:
                    RuntimeInformation.OSDescription,

                Architecture:
                    RuntimeInformation.OSArchitecture.ToString(),

                Framework:
                    RuntimeInformation.FrameworkDescription);

        await File.WriteAllTextAsync(
            markerPath,
            JsonSerializer.Serialize(
                marker,
                new JsonSerializerOptions
                {
                    WriteIndented =
                        true
                }));
    }

    private void TogglePanel()
    {
        if (_panel is null)
        {
            return;
        }

        if (_panel.IsVisible)
        {
            _panel.Hide();

            _systemPolicies?.SetPanelVisibility(
                isVisible:
                    false);
        }
        else
        {
            ShowPanel();
        }
    }

    private void ShowPanel()
    {
        if (_panel is null)
        {
            return;
        }

        _panel.Show();

        _systemPolicies?.SetPanelVisibility(
            isVisible:
                true);

        _panel.Activate();
    }

    private void ExitApplication()
    {
        _panel?.AllowCloseAndExit();

        Shutdown();
    }
}

public sealed record SelfTestMarker(
    string Status,
    DateTimeOffset TimestampUtc,
    string ExecutablePath,
    string OperatingSystem,
    string Architecture,
    string Framework);

public sealed record LaunchOptions(
    bool ShowPanel,
    TimeSpan StartupDelay,
    string? SelfTestMarkerPath)
{
    public static LaunchOptions Parse(
        IReadOnlyList<string> arguments)
    {
        var showPanel =
            arguments.Any(
                argument =>
                    string.Equals(
                        argument,
                        "--show-panel",
                        StringComparison.OrdinalIgnoreCase));

        var delaySeconds =
            0;

        string? selfTestMarkerPath =
            null;

        for (var index = 0;
            index < arguments.Count - 1;
            index++)
        {
            if (string.Equals(
                arguments[index],
                "--startup-delay-seconds",
                StringComparison.OrdinalIgnoreCase)
                && int.TryParse(
                    arguments[index + 1],
                    out var parsedDelay))
            {
                delaySeconds =
                    Math.Max(
                        0,
                        parsedDelay);
            }

            if (string.Equals(
                arguments[index],
                "--self-test-marker",
                StringComparison.OrdinalIgnoreCase))
            {
                selfTestMarkerPath =
                    arguments[index + 1];
            }
        }

        return new LaunchOptions(
            showPanel,
            TimeSpan.FromSeconds(
                delaySeconds),

            selfTestMarkerPath);
    }
}