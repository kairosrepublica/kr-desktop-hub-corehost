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
    private WidgetManagerWindow? _widgetManagerWindow;
    private SettingsCenterWindow? _settingsCenterWindow;
    private SettingsCenterRuntimeBridge? _settingsCenterBridge;
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
    private InternalWidgetManagerService? _widgetManager;
    private InstalledWidgetHostCompositionCoordinator? _widgetHostCoordinator;
    private WindowsInstalledWidgetVisualSurfaceRegistry? _widgetVisualSurfaces;
    private WindowsWidgetFrameworkServices? _widgetFrameworkServices;

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

            _panel.CloseToTrayRequested +=
                (_, _) =>
                    HidePanel();

            _panel.WidgetManagerRequested +=
                (_, _) =>
                    ShowWidgetManager();

            _panel.SettingsCenterRequested +=
                (_, _) =>
                    ShowSettingsCenter();

            _tray =
                new WindowsTrayService();

            await _tray.InitializeAsync(
                CancellationToken.None);

            await _tray.SetStatusAsync(
                new TrayStatus(
                    CoreHostTrayStatusText.Ready,
                    VisualState:
                        WindowsTrayVisualStateCatalog.Default),

                CancellationToken.None);

            InitializeWidgetFramework(
                dataRoot);

            await RefreshWidgetHostAsync();

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

            _tray.WidgetManagerRequested +=
                (_, _) =>
                    ShowWidgetManager();

            _tray.SettingsCenterRequested +=
                (_, _) =>
                    ShowSettingsCenter();

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

    private void ShowSettingsCenter()
    {
        if (_panel is null)
        {
            return;
        }

        _settingsCenterBridge ??=
            new SettingsCenterRuntimeBridge(
                CoreHostDataRootResolver
                    .ResolveDefaultDataRoot());

        if (_settingsCenterWindow is null)
        {
            _settingsCenterWindow =
                new SettingsCenterWindow(
                    _settingsCenterBridge);

            _settingsCenterWindow.Owner =
                _panel;

            _settingsCenterWindow.SettingsSaved +=
                async (_, _) =>
                    await ReloadSettingsAsync();

            _settingsCenterWindow.Closed +=
                (_, _) =>
                    _settingsCenterWindow =
                        null;
        }

        _settingsCenterWindow.Show();
        _settingsCenterWindow.Activate();
    }

    private void ShowWidgetManager()
    {
        if (_panel is null
            || _widgetManager is null
            || _widgetHostCoordinator is null)
        {
            return;
        }

        if (_widgetManagerWindow is null)
        {
            _widgetManagerWindow =
                new WidgetManagerWindow(
                    _widgetManager,

                    () =>
                        CreateWidgetManager(
                            allowDevelopmentFolderInstall:
                                true,

                            installedCatalog:
                                _widgetManager
                                    .InstalledCatalog),

                    cancellationToken =>
                        _widgetHostCoordinator
                            .RefreshAsync(
                                cancellationToken));

            _widgetManagerWindow.Owner =
                _panel;

            _widgetManagerWindow.InstalledWidgetStateChanged +=
                async (_, _) =>
                    await SynchronizeWidgetHostStateAsync();

            _widgetManagerWindow.Closed +=
                (_, _) =>
                    _widgetManagerWindow =
                        null;
        }

        _widgetManagerWindow.Show();
        _widgetManagerWindow.Activate();
    }

    private void InitializeWidgetFramework(
        string dataRoot)
    {
        if (_panel is null
            || _tray is null)
        {
            throw new InvalidOperationException(
                "Panel and tray must exist before Widget framework initialization.");
        }

        var layoutController =
            new WidgetHostLayoutController(
                new JsonWidgetHostStateStore(
                    Path.Combine(
                        dataRoot,
                        "state",
                        "widget-host-state.json")));

        _widgetManager =
            CreateWidgetManager(
                allowDevelopmentFolderInstall:
                    false,

                installedCatalog:
                    null,

                layoutController:
                    layoutController);

        _widgetVisualSurfaces =
            new WindowsInstalledWidgetVisualSurfaceRegistry();

        _widgetFrameworkServices =
            new WindowsWidgetFrameworkServices(
                _panel,
                _tray,
                layoutController);

        _widgetHostCoordinator =
            new InstalledWidgetHostCompositionCoordinator(
                _panel,
                _widgetManager,
                _widgetVisualSurfaces,
                _widgetFrameworkServices);

        _widgetHostCoordinator.OperationFailed +=
            async (_, failure) =>
                await PublishNotificationAsync(
                    new SystemNotification(
                        "widget.host.operation.failed",
                        "KR Desktop Hub",
                        failure.Operation
                        + " failed: "
                        + failure.Exception.Message,
                        NotificationPriority.Important,
                        Array.Empty<NotificationAction>()),

                    force:
                        true);
    }

    private static InternalWidgetManagerService CreateWidgetManager(
        bool allowDevelopmentFolderInstall,
        InstalledWidgetCatalogService? installedCatalog =
            null,
        WidgetHostLayoutController? layoutController =
            null)
    {
        var dataRoot =
            CoreHostDataRootResolver
                .ResolveDefaultDataRoot();

        var options =
            WidgetPackageInstallerOptions
                .CreateRecommended(
                    dataRoot,

                    new Version(
                        0,
                        1,
                        0),

                    allowedCapabilities:
                        WidgetCapabilityCatalog.PackageApprovableIds);

        if (allowDevelopmentFolderInstall)
        {
            options =
                options with
                {
                    AllowDevelopmentFolderInstall =
                        true
                };
        }

        var installer =
            new InternalWidgetPackageInstaller(
                options);

        installedCatalog ??=
            new InstalledWidgetCatalogService(
                installer.InstalledDirectory,
                layoutController
                ?? new WidgetHostLayoutController(
                    new JsonWidgetHostStateStore(
                        Path.Combine(
                            dataRoot,
                            "state",
                            "widget-host-state.json"))));

        return new InternalWidgetManagerService(
            installer,
            installedCatalog);
    }

    private async Task SynchronizeWidgetHostStateAsync()
    {
        if (_widgetHostCoordinator is null)
        {
            return;
        }

        try
        {
            await _widgetHostCoordinator
                .SynchronizeStateAsync(
                    CancellationToken.None);
        }
        catch (Exception exception)
        {
            await PublishNotificationAsync(
                new SystemNotification(
                    "widget.host.state.synchronize.failed",
                    "KR Desktop Hub",
                    $"Widget host state synchronization failed: {exception.Message}",
                    NotificationPriority.Important,
                    Array.Empty<NotificationAction>()),

                force:
                    true);
        }
    }

    private async Task RefreshWidgetHostAsync()
    {
        if (_widgetHostCoordinator is null)
        {
            return;
        }

        try
        {
            await _widgetHostCoordinator
                .RefreshAsync(
                    CancellationToken.None);
        }
        catch (Exception exception)
        {
            await PublishNotificationAsync(
                new SystemNotification(
                    "widget.host.refresh.failed",
                    "KR Desktop Hub",
                    $"Widget host refresh failed: {exception.Message}",
                    NotificationPriority.Important,
                    Array.Empty<NotificationAction>()),

                force:
                    true);
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
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    private void HidePanel()
    {
        if (_panel is null)
        {
            return;
        }

        _panel.Hide();

        _systemPolicies?.SetPanelVisibility(
            isVisible:
                false);
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
