using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;
using FormsScreen = System.Windows.Forms.Screen;
using WpfWindow = System.Windows.Window;
using WpfWindowState = System.Windows.WindowState;

namespace KRDesktopHub.Platform.Windows;

public sealed record WindowPlacementState(
    int SchemaVersion,
    string PanelId,
    double Left,
    double Top,
    double Width,
    double Height,
    string WindowState,
    string MonitorDeviceName,
    DateTimeOffset SavedAtUtc);

public sealed record MonitorWorkingArea(
    string DeviceName,
    double Left,
    double Top,
    double Width,
    double Height,
    bool IsPrimary)
{
    public double Right =>
        Left + Width;

    public double Bottom =>
        Top + Height;
}

public sealed record WindowPlacementDefaults(
    double DefaultWidth,
    double DefaultHeight,
    double MinimumWidth,
    double MinimumHeight)
{
    public static WindowPlacementDefaults Recommended =>
        new(
            DefaultWidth:
                360,

            DefaultHeight:
                720,

            MinimumWidth:
                260,

            MinimumHeight:
                320);
}

public static class CoreHostDataRootResolver
{
    public const string OverrideEnvironmentVariable =
        "KRDESKTOPHUB_DATA_ROOT";

    public static string ResolveDefaultDataRoot()
    {
        var configured =
            Environment.GetEnvironmentVariable(
                OverrideEnvironmentVariable);

        if (!string.IsNullOrWhiteSpace(
            configured))
        {
            return Path.GetFullPath(
                configured);
        }

        var documents =
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments);

        if (string.IsNullOrWhiteSpace(
            documents))
        {
            documents =
                AppContext.BaseDirectory;
        }

        return Path.Combine(
            documents,
            "KRG",
            "KRG Dock",
            "KRG App",
            "KRDesktopHub");
    }
}

public sealed class JsonWindowPlacementStore
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

    public JsonWindowPlacementStore(
        string stateFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            stateFilePath);

        StateFilePath =
            stateFilePath;
    }

    public string StateFilePath { get; }

    public WindowPlacementState? TryLoad()
    {
        try
        {
            if (!File.Exists(
                StateFilePath))
            {
                return null;
            }

            return JsonSerializer.Deserialize<WindowPlacementState>(
                File.ReadAllText(
                    StateFilePath),

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

    public void Save(
        WindowPlacementState state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        var directory =
            Path.GetDirectoryName(
                StateFilePath);

        if (!string.IsNullOrWhiteSpace(
            directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        lock (_saveGate)
        {
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

public static class WindowPlacementPolicy
{
    public const int CurrentSchemaVersion =
        1;

    public const string MainPanelId =
        "main-panel";

    public static WindowPlacementState Normalize(
        WindowPlacementState? candidate,
        IReadOnlyList<MonitorWorkingArea> monitors,
        WindowPlacementDefaults defaults)
    {
        ArgumentNullException.ThrowIfNull(
            monitors);

        ArgumentNullException.ThrowIfNull(
            defaults);

        if (
            candidate is not null
            && (
                candidate.SchemaVersion !=
                    CurrentSchemaVersion
                || !string.Equals(
                    candidate.PanelId,
                    MainPanelId,
                    StringComparison.Ordinal)
            )
        )
        {
            candidate =
                null;
        }

        var availableMonitors =
            monitors.Count > 0
                ? monitors
                : new[]
                {
                    new MonitorWorkingArea(
                        "fallback",
                        0,
                        0,
                        1920,
                        1080,
                        IsPrimary:
                            true)
                };

        var selectedMonitor =
            SelectMonitor(
                candidate,
                availableMonitors);

        var rawWidth =
            IsUsable(
                candidate?.Width)
                ? candidate!.Width
                : defaults.DefaultWidth;

        var rawHeight =
            IsUsable(
                candidate?.Height)
                ? candidate!.Height
                : defaults.DefaultHeight;

        var maximumWidth =
            Math.Max(
                1,
                selectedMonitor.Width);

        var maximumHeight =
            Math.Max(
                1,
                selectedMonitor.Height);

        var minimumWidth =
            Math.Min(
                defaults.MinimumWidth,
                maximumWidth);

        var minimumHeight =
            Math.Min(
                defaults.MinimumHeight,
                maximumHeight);

        var width =
            Math.Clamp(
                rawWidth,
                minimumWidth,
                maximumWidth);

        var height =
            Math.Clamp(
                rawHeight,
                minimumHeight,
                maximumHeight);

        var rawLeft =
            IsUsable(
                candidate?.Left)
                ? candidate!.Left
                : selectedMonitor.Left;

        var rawTop =
            IsUsable(
                candidate?.Top)
                ? candidate!.Top
                : selectedMonitor.Top;

        var left =
            Math.Clamp(
                rawLeft,
                selectedMonitor.Left,
                selectedMonitor.Right - width);

        var top =
            Math.Clamp(
                rawTop,
                selectedMonitor.Top,
                selectedMonitor.Bottom - height);

        return new WindowPlacementState(
            SchemaVersion:
                CurrentSchemaVersion,

            PanelId:
                MainPanelId,

            Left:
                left,

            Top:
                top,

            Width:
                width,

            Height:
                height,

            WindowState:
                string.Equals(
                    candidate?.WindowState,
                    nameof(
                        WpfWindowState.Maximized),
                    StringComparison.OrdinalIgnoreCase)
                    ? nameof(
                        WpfWindowState.Maximized)
                    : nameof(
                        WpfWindowState.Normal),

            MonitorDeviceName:
                selectedMonitor.DeviceName,

            SavedAtUtc:
                DateTimeOffset.UtcNow);
    }

    public static IReadOnlyList<MonitorWorkingArea> GetCurrentMonitors()
    {
        return FormsScreen
            .AllScreens
            .Select(
                screen =>
                    new MonitorWorkingArea(
                        screen.DeviceName,
                        screen.WorkingArea.Left,
                        screen.WorkingArea.Top,
                        screen.WorkingArea.Width,
                        screen.WorkingArea.Height,
                        screen.Primary))
            .ToArray();
    }

    private static MonitorWorkingArea SelectMonitor(
        WindowPlacementState? candidate,
        IReadOnlyList<MonitorWorkingArea> monitors)
    {
        if (!string.IsNullOrWhiteSpace(
            candidate?.MonitorDeviceName))
        {
            var sameMonitor =
                monitors.FirstOrDefault(
                    monitor =>
                        string.Equals(
                            monitor.DeviceName,
                            candidate.MonitorDeviceName,
                            StringComparison.OrdinalIgnoreCase));

            if (sameMonitor is not null)
            {
                return sameMonitor;
            }
        }

        if (
            candidate is not null
            && IsUsable(
                candidate.Left)
            && IsUsable(
                candidate.Top)
            && IsUsable(
                candidate.Width)
            && IsUsable(
                candidate.Height)
        )
        {
            var bestIntersection =
                monitors
                    .Select(
                        monitor =>
                            new
                            {
                                Monitor =
                                    monitor,

                                Area =
                                    GetIntersectionArea(
                                        candidate,
                                        monitor)
                            })
                    .OrderByDescending(
                        item =>
                            item.Area)
                    .First();

            if (bestIntersection.Area > 0)
            {
                return bestIntersection.Monitor;
            }
        }

        return monitors.FirstOrDefault(
            monitor =>
                monitor.IsPrimary)
            ?? monitors[0];
    }

    private static double GetIntersectionArea(
        WindowPlacementState placement,
        MonitorWorkingArea monitor)
    {
        var left =
            Math.Max(
                placement.Left,
                monitor.Left);

        var top =
            Math.Max(
                placement.Top,
                monitor.Top);

        var right =
            Math.Min(
                placement.Left + placement.Width,
                monitor.Right);

        var bottom =
            Math.Min(
                placement.Top + placement.Height,
                monitor.Bottom);

        return Math.Max(
                0,
                right - left)
            * Math.Max(
                0,
                bottom - top);
    }

    private static bool IsUsable(
        double? value)
    {
        return value.HasValue
            && double.IsFinite(
                value.Value);
    }
}

public sealed class WindowsWindowPlacementService
    : IDisposable
{
    private readonly JsonWindowPlacementStore _store;
    private readonly WindowPlacementDefaults _defaults;
    private readonly TimeSpan _debounce;
    private readonly object _gate =
        new();

    private System.Threading.Timer? _timer;
    private WpfWindow? _window;
    private WindowPlacementState? _pendingState;
    private bool _isApplying;
    private bool _disposed;

    public WindowsWindowPlacementService(
        string dataRoot,
        WindowPlacementDefaults? defaults =
            null,
        TimeSpan? debounce =
            null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataRoot);

        _store =
            new JsonWindowPlacementStore(
                Path.Combine(
                    dataRoot,
                    "state",
                    "window-placement.json"));

        _defaults =
            defaults
            ?? WindowPlacementDefaults.Recommended;

        _debounce =
            debounce
            ?? TimeSpan.FromMilliseconds(
                600);
    }

    public string StateFilePath =>
        _store.StateFilePath;

    public Exception? LastSaveError { get; private set; }

    public void Attach(
        WpfWindow window)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(
                    WindowsWindowPlacementService));
        }

        ArgumentNullException.ThrowIfNull(
            window);

        if (_window is not null)
        {
            throw new InvalidOperationException(
                "A window-placement service instance can attach only one window.");
        }

        _window =
            window;

        ApplySavedPlacement();

        window.LocationChanged +=
            OnPlacementChanged;

        window.SizeChanged +=
            OnPlacementChanged;

        window.StateChanged +=
            OnPlacementChanged;

        window.IsVisibleChanged +=
            OnIsVisibleChanged;

        window.Closing +=
            OnClosing;
    }

    public void SaveNow()
    {
        if (_disposed)
        {
            return;
        }

        var state =
            CaptureCurrentPlacement();

        lock (_gate)
        {
            _pendingState =
                null;

            _timer?.Change(
                Timeout.Infinite,
                Timeout.Infinite);
        }

        if (state is not null)
        {
            TrySave(
                state);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        SaveNow();

        if (_window is not null)
        {
            _window.LocationChanged -=
                OnPlacementChanged;

            _window.SizeChanged -=
                OnPlacementChanged;

            _window.StateChanged -=
                OnPlacementChanged;

            _window.IsVisibleChanged -=
                OnIsVisibleChanged;

            _window.Closing -=
                OnClosing;
        }

        lock (_gate)
        {
            _timer?.Dispose();
            _timer =
                null;

            _pendingState =
                null;
        }

        _disposed =
            true;
    }

    private void ApplySavedPlacement()
    {
        if (_window is null)
        {
            return;
        }

        var placement =
            WindowPlacementPolicy.Normalize(
                _store.TryLoad(),
                WindowPlacementPolicy.GetCurrentMonitors(),
                _defaults);

        _isApplying =
            true;

        try
        {
            _window.WindowStartupLocation =
                WindowStartupLocation.Manual;

            _window.Left =
                placement.Left;

            _window.Top =
                placement.Top;

            _window.Width =
                placement.Width;

            _window.Height =
                placement.Height;

            _window.WindowState =
                string.Equals(
                    placement.WindowState,
                    nameof(
                        WpfWindowState.Maximized),
                    StringComparison.OrdinalIgnoreCase)
                    ? WpfWindowState.Maximized
                    : WpfWindowState.Normal;
        }
        finally
        {
            _isApplying =
                false;
        }
    }

    private void OnPlacementChanged(
        object? sender,
        EventArgs eventArgs)
    {
        ScheduleSave();
    }

    private void OnIsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (
            _window is not null
            && !_window.IsVisible
        )
        {
            SaveNow();
        }
    }

    private void OnClosing(
        object? sender,
        System.ComponentModel.CancelEventArgs eventArgs)
    {
        SaveNow();
    }

    private void ScheduleSave()
    {
        if (
            _disposed
            || _isApplying
        )
        {
            return;
        }

        var state =
            CaptureCurrentPlacement();

        if (state is null)
        {
            return;
        }

        lock (_gate)
        {
            _pendingState =
                state;

            _timer ??=
                new System.Threading.Timer(
                    _ =>
                        FlushPending(),

                    null,
                    Timeout.Infinite,
                    Timeout.Infinite);

            _timer.Change(
                _debounce,
                Timeout.InfiniteTimeSpan);
        }
    }

    private void FlushPending()
    {
        WindowPlacementState? state;

        lock (_gate)
        {
            state =
                _pendingState;

            _pendingState =
                null;
        }

        if (state is not null)
        {
            TrySave(
                state);
        }
    }

    private WindowPlacementState? CaptureCurrentPlacement()
    {
        if (_window is null)
        {
            return null;
        }

        var rectangle =
            _window.WindowState ==
                WpfWindowState.Normal
                ? new Rect(
                    _window.Left,
                    _window.Top,
                    _window.Width,
                    _window.Height)
                : _window.RestoreBounds;

        var state =
            new WindowPlacementState(
                SchemaVersion:
                    WindowPlacementPolicy.CurrentSchemaVersion,

                PanelId:
                    WindowPlacementPolicy.MainPanelId,

                Left:
                    rectangle.Left,

                Top:
                    rectangle.Top,

                Width:
                    rectangle.Width,

                Height:
                    rectangle.Height,

                WindowState:
                    _window.WindowState ==
                        WpfWindowState.Maximized
                        ? nameof(
                            WpfWindowState.Maximized)
                        : nameof(
                            WpfWindowState.Normal),

                MonitorDeviceName:
                    string.Empty,

                SavedAtUtc:
                    DateTimeOffset.UtcNow);

        return WindowPlacementPolicy.Normalize(
            state,
            WindowPlacementPolicy.GetCurrentMonitors(),
            _defaults);
    }

    private void TrySave(
        WindowPlacementState state)
    {
        try
        {
            _store.Save(
                state);

            LastSaveError =
                null;
        }
        catch (
            Exception exception)
            when (
                exception is IOException
                or UnauthorizedAccessException
                or JsonException)
        {
            LastSaveError =
                exception;
        }
    }
}