using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using KRDesktopHub.Contracts;
using KRDesktopHub.Platform.Abstractions;
using Forms = System.Windows.Forms;

namespace KRDesktopHub.Platform.Windows;

public sealed class WindowsTrayService : ITrayService
{
    private readonly Forms.NotifyIcon _notifyIcon = new();
    private bool _initialized;

    public event EventHandler? ToggleRequested;

    public event EventHandler? ExitRequested;

    public event EventHandler? TestNotificationRequested;

    public event EventHandler? StartupToggleRequested;

    public event EventHandler? SettingsReloadRequested;

    public event EventHandler? SettingsFolderRequested;

    public event EventHandler? WidgetManagerRequested;

    public Task InitializeAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_initialized)
        {
            return Task.CompletedTask;
        }

        var menu = new Forms.ContextMenuStrip();

        menu.Items.Add(
            "Show or Hide Panel",
            image: null,
            (_, _) => ToggleRequested?.Invoke(this, EventArgs.Empty));

        menu.Items.Add(
            "Open Widget Manager",
            image: null,
            (_, _) => WidgetManagerRequested?.Invoke(this, EventArgs.Empty));

        menu.Items.Add(
            "Send Test Notification",
            image: null,
            (_, _) => TestNotificationRequested?.Invoke(this, EventArgs.Empty));

        menu.Items.Add(
            "Enable or Disable Startup",
            image: null,
            (_, _) => StartupToggleRequested?.Invoke(this, EventArgs.Empty));

        menu.Items.Add(
            "Reload Settings",
            image: null,
            (_, _) => SettingsReloadRequested?.Invoke(this, EventArgs.Empty));

        menu.Items.Add(
            "Open Settings Folder",
            image: null,
            (_, _) => SettingsFolderRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new Forms.ToolStripSeparator());

        menu.Items.Add(
            "Exit",
            image: null,
            (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon.Icon = SystemIcons.Application;
        _notifyIcon.Text = "KR Desktop Hub";
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.Visible = true;
        _notifyIcon.DoubleClick +=
            (_, _) => ToggleRequested?.Invoke(this, EventArgs.Empty);

        _initialized = true;

        return Task.CompletedTask;
    }

    public Task SetStatusAsync(
        TrayStatus status,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(status);

        _notifyIcon.Text = LimitTooltip(status.Tooltip);

        return Task.CompletedTask;
    }

    public Task ShowBalloonAsync(
        string title,
        string message,
        Forms.ToolTipIcon icon,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _notifyIcon.ShowBalloonTip(
            timeout: 4000,
            tipTitle: title,
            tipText: message,
            tipIcon: icon);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        return Task.CompletedTask;
    }

    private static string LimitTooltip(string tooltip)
    {
        const int maximumLength = 63;

        return tooltip.Length <= maximumLength
            ? tooltip
            : tooltip[..maximumLength];
    }
}

public sealed class WindowsTrayBalloonNotificationService
    : ISystemNotificationService
{
    private readonly WindowsTrayService _tray;

    public WindowsTrayBalloonNotificationService(
        WindowsTrayService tray)
    {
        _tray = tray;
    }

    public Task PublishAsync(
        SystemNotification notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var icon = notification.Priority switch
        {
            NotificationPriority.Urgent =>
                Forms.ToolTipIcon.Error,

            NotificationPriority.Important =>
                Forms.ToolTipIcon.Warning,

            _ =>
                Forms.ToolTipIcon.Info
        };

        return _tray.ShowBalloonAsync(
            notification.Title,
            notification.Message,
            icon,
            cancellationToken);
    }
}

public sealed class WindowsGlobalHotkeyService
    : IGlobalHotkeyService, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModifierAlt = 0x0001;
    private const uint ModifierControl = 0x0002;
    private const uint ModifierShift = 0x0004;
    private const uint ModifierWindows = 0x0008;

    private readonly Dictionary<int, string> _commands = new();
    private readonly HashSet<string> _registeredCommands =
        new(StringComparer.OrdinalIgnoreCase);

    private HwndSource? _source;
    private IntPtr _windowHandle;
    private int _nextId = 0x5000;

    public event EventHandler<string>? CommandInvoked;

    public void Attach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_source is not null)
        {
            return;
        }

        _windowHandle =
            new WindowInteropHelper(window)
                .EnsureHandle();

        _source =
            HwndSource.FromHwnd(_windowHandle)
            ?? throw new InvalidOperationException(
                "Unable to attach hotkey message hook.");

        _source.AddHook(WndProc);
    }

    public Task RegisterAsync(
        HotkeyRegistration registration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(registration);

        if (_windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Attach a WPF Window before registering hotkeys.");
        }

        if (!_registeredCommands.Add(registration.CommandId))
        {
            throw new InvalidOperationException(
                $"Hotkey command already registered: {registration.CommandId}");
        }

        var parsed = ParseGesture(registration.Gesture);
        var id = _nextId++;

        if (!RegisterHotKey(
            _windowHandle,
            id,
            parsed.Modifiers,
            parsed.VirtualKey))
        {
            _registeredCommands.Remove(registration.CommandId);

            var errorCode =
                Marshal.GetLastWin32Error();

            throw new InvalidOperationException(
                $"Unable to register hotkey: {registration.Gesture}. Windows error code: {errorCode}");
        }

        _commands[id] = registration.CommandId;

        return Task.CompletedTask;
    }

    public Task UnregisterAllAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var id in _commands.Keys.ToArray())
        {
            UnregisterHotKey(_windowHandle, id);
        }

        _commands.Clear();
        _registeredCommands.Clear();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _ = UnregisterAllAsync(CancellationToken.None);

        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }

    public static ParsedHotkey ParseGesture(string gesture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gesture);

        var parts = gesture
            .Split(
                '+',
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            throw new FormatException(
                $"Hotkey must include a modifier and key: {gesture}");
        }

        uint modifiers = 0;
        Key? key = null;

        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= ModifierControl;
                    break;

                case "ALT":
                    modifiers |= ModifierAlt;
                    break;

                case "SHIFT":
                    modifiers |= ModifierShift;
                    break;

                case "WIN":
                case "WINDOWS":
                    modifiers |= ModifierWindows;
                    break;

                default:
                    if (key is not null)
                    {
                        throw new FormatException(
                            $"Hotkey contains multiple keys: {gesture}");
                    }

                    key = (Key)new KeyConverter()
                        .ConvertFromString(part)!;

                    break;
            }
        }

        if (modifiers == 0 || key is null)
        {
            throw new FormatException(
                $"Hotkey is incomplete: {gesture}");
        }

        return new ParsedHotkey(
            modifiers,
            (uint)KeyInterop.VirtualKeyFromKey(key.Value));
    }

    private IntPtr WndProc(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message != WmHotkey)
        {
            return IntPtr.Zero;
        }

        var id = wParam.ToInt32();

        if (_commands.TryGetValue(id, out var commandId))
        {
            CommandInvoked?.Invoke(this, commandId);
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(
        IntPtr windowHandle,
        int id,
        uint modifiers,
        uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(
        IntPtr windowHandle,
        int id);
}

public sealed record ParsedHotkey(
    uint Modifiers,
    uint VirtualKey);

public static partial class StartupCommandBuilder
{
    private static readonly Regex DelayRegex =
        DelayArgumentRegex();

    public static string Build(
        string executablePath,
        StartupRegistration registration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(registration);

        var delaySeconds =
            Math.Max(
                0,
                (int)Math.Round(
                    registration.Delay.TotalSeconds));

        return
            $"\"{executablePath}\" --start-hidden --startup-delay-seconds {delaySeconds}";
    }

    public static TimeSpan ParseDelay(
        string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return TimeSpan.Zero;
        }

        var match = DelayRegex.Match(command);

        return match.Success
            ? TimeSpan.FromSeconds(
                int.Parse(match.Groups["seconds"].Value))
            : TimeSpan.Zero;
    }

    [GeneratedRegex(
        @"--startup-delay-seconds\s+(?<seconds>\d+)",
        RegexOptions.IgnoreCase)]
    private static partial Regex DelayArgumentRegex();
}

public sealed class WindowsStartupRegistrationService
    : IStartupRegistrationService
{
    private const string RunKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const string ValueName =
        "KRDesktopHub";

    private readonly string _executablePath;

    public WindowsStartupRegistrationService(
        string executablePath)
    {
        _executablePath = executablePath;
    }

    public Task<StartupRegistration> GetAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var key =
            Registry.CurrentUser.OpenSubKey(
                RunKeyPath,
                writable: false);

        var command =
            key?.GetValue(ValueName) as string;

        return Task.FromResult(
            new StartupRegistration(
                Enabled:
                    !string.IsNullOrWhiteSpace(command),

                Delay:
                    StartupCommandBuilder.ParseDelay(command)));
    }

    public Task SetAsync(
        StartupRegistration registration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(registration);

        using var key =
            Registry.CurrentUser.CreateSubKey(
                RunKeyPath,
                writable: true);

        if (registration.Enabled)
        {
            key.SetValue(
                ValueName,
                StartupCommandBuilder.Build(
                    _executablePath,
                    registration));
        }
        else
        {
            key.DeleteValue(
                ValueName,
                throwOnMissingValue: false);
        }

        return Task.CompletedTask;
    }
}

public sealed class WindowsPrivilegeService
    : IPrivilegeService
{
    public bool IsElevated
    {
        get
        {
            using var identity =
                WindowsIdentity.GetCurrent();

            var principal =
                new WindowsPrincipal(identity);

            return principal.IsInRole(
                WindowsBuiltInRole.Administrator);
        }
    }

    public Task<bool> RequestElevationAsync(
        string reason,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var executablePath =
            Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return Task.FromResult(false);
        }

        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = "--elevated"
                });

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

public sealed class WindowsPlatformInfoService
    : IPlatformInfoService
{
    public string OperatingSystem =>
        RuntimeInformation.OSDescription;

    public string Architecture =>
        RuntimeInformation.OSArchitecture.ToString();

    public string RuntimeVersion =>
        RuntimeInformation.FrameworkDescription;
}