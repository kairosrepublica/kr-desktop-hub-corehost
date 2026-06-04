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
    private WindowsPowerStateService? _power;
    private WindowsNetworkStateService? _network;
    private WindowsSessionStateService? _session;
    private WindowsTimeZoneChangeService? _timeZone;
    private WindowsProcessResourceMonitorService? _resources;
    private SystemPolicyCoordinator? _systemPolicies;

    protected override async void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _mutex =
                new Mutex(
                    initiallyOwned: true,
                    name: MutexName,
                    createdNew: out _ownsMutex);

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

            var options =
                LaunchOptions.Parse(e.Args);

            if (options.StartupDelay > TimeSpan.Zero)
            {
                await Task.Delay(options.StartupDelay);
            }

            _panel =
                new MainWindow();

            _tray =
                new WindowsTrayService();

            await _tray.InitializeAsync(
                CancellationToken.None);

            await _tray.SetStatusAsync(
                new TrayStatus(
                    "KR Desktop Hub â€” Ready"),
                CancellationToken.None);

            _notifications =
                new WindowsTrayBalloonNotificationService(
                    _tray);

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
                    CoreHostPolicyOptions
                        .Recommended
                        .ResourceSampleInterval);

            _systemPolicies =
                new SystemPolicyCoordinator(
                    new InMemoryEventBus(),
                    _power,
                    _network,
                    _session,
                    _timeZone,
                    _resources);

            await _resources.StartAsync(
                CancellationToken.None);

            _startup =
                new WindowsStartupRegistrationService(
                    Environment.ProcessPath
                    ?? throw new InvalidOperationException(
                        "Unable to determine executable path."));

            _hotkeys =
                new WindowsGlobalHotkeyService();

            _hotkeys.Attach(_panel);

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

            try
            {
                await _hotkeys.RegisterAsync(
                    new HotkeyRegistration(
                        "panel.toggle",
                        "Ctrl+Alt+K"),
                    CancellationToken.None);
            }
            catch (Exception hotkeyException)
            {
                await _notifications.PublishAsync(
                    new SystemNotification(
                        "hotkey.registration.failed",
                        "KR Desktop Hub",
                        $"Global hotkey registration failed: {hotkeyException.Message}",
                        NotificationPriority.Important,
                        Array.Empty<NotificationAction>()),
                    CancellationToken.None);
            }

            _tray.ToggleRequested +=
                (_, _) => TogglePanel();

            _tray.ExitRequested +=
                (_, _) => ExitApplication();

            _tray.TestNotificationRequested +=
                async (_, _) =>
                    await _notifications.PublishAsync(
                        new SystemNotification(
                            "notification.test",
                            "KR Desktop Hub",
                            "System-tray notification is working.",
                            NotificationPriority.Informational,
                            Array.Empty<NotificationAction>()),
                        CancellationToken.None);

            _tray.StartupToggleRequested +=
                async (_, _) =>
                {
                    var current =
                        await _startup.GetAsync(
                            CancellationToken.None);

                    var next =
                        new StartupRegistration(
                            Enabled: !current.Enabled,
                            Delay: TimeSpan.FromSeconds(10));

                    await _startup.SetAsync(
                        next,
                        CancellationToken.None);

                    await _notifications.PublishAsync(
                        new SystemNotification(
                            "startup.registration.changed",
                            "KR Desktop Hub",
                            next.Enabled
                                ? "Launch at login is enabled."
                                : "Launch at login is disabled.",
                            NotificationPriority.Informational,
                            Array.Empty<NotificationAction>()),
                        CancellationToken.None);
                };

            if (options.ShowPanel)
            {
                ShowPanel();
            }
            else
            {
                _panel.Hide();
                _systemPolicies.SetPanelVisibility(
                    isVisible: false);
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

        base.OnExit(e);
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
                isVisible: false);
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
            isVisible: true);

        _panel.Activate();
    }

    private void ExitApplication()
    {
        _panel?.AllowCloseAndExit();
        Shutdown();
    }
}

public sealed record LaunchOptions(
    bool ShowPanel,
    TimeSpan StartupDelay)
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

        var delaySeconds = 0;

        for (var index = 0;
            index < arguments.Count - 1;
            index++)
        {
            if (!string.Equals(
                arguments[index],
                "--startup-delay-seconds",
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(
                arguments[index + 1],
                out var parsedDelay))
            {
                delaySeconds =
                    Math.Max(
                        0,
                        parsedDelay);
            }
        }

        return new LaunchOptions(
            showPanel,
            TimeSpan.FromSeconds(delaySeconds));
    }
}