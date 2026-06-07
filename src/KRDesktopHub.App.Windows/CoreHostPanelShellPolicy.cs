using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;


namespace KRDesktopHub.App.Windows;

public static class CoreHostPanelShellPolicy
{
    public const bool ShowActivated =
        false;

    public const bool ShowInTaskbar =
        false;

    public const bool ForceActivateAfterOrdinaryShow =
        false;

    public const bool UseNoActivateExtendedStyle =
        true;

    public const bool HideOnMinimize =
        true;

    public const bool CollapseToggleReceivesKeyboardFocus =
        false;
}

public sealed class CoreHostPanelNativeShellAdapter
    : IDisposable
{
    private const int GwlExStyle =
        -20;

    private const int WsExNoActivate =
        0x08000000;

    private const int WmSysCommand =
        0x0112;

    private const int ScMinimize =
        0xF020;

    private readonly Window _window;
    private readonly Action _minimizeRequested;
    private HwndSource? _source;
    private bool _disposed;

    public CoreHostPanelNativeShellAdapter(
        Window window,
        Action minimizeRequested)
    {
        _window =
            window
            ?? throw new ArgumentNullException(
                nameof(window));

        _minimizeRequested =
            minimizeRequested
            ?? throw new ArgumentNullException(
                nameof(minimizeRequested));

        _window.SourceInitialized +=
            HandleSourceInitialized;
    }

    public bool NoActivateExtendedStyleApplied { get; private set; }

    public void AttachIfReady()
    {
        if (
            _disposed
            || _source is not null
        )
        {
            return;
        }

        var handle =
            new WindowInteropHelper(
                _window)
                .Handle;

        if (handle == IntPtr.Zero)
        {
            return;
        }

        var source =
            HwndSource.FromHwnd(
                handle)
            ?? throw new InvalidOperationException(
                "CoreHost popup HWND source was not available.");

        if (CoreHostPanelShellPolicy.UseNoActivateExtendedStyle)
        {
            var current =
                GetWindowLongPtrSafe(
                    handle,
                    GwlExStyle);

            var next =
                new IntPtr(
                    current.ToInt64()
                    | WsExNoActivate);

            _ =
                SetWindowLongPtrSafe(
                    handle,
                    GwlExStyle,
                    next);

            NoActivateExtendedStyleApplied =
                true;
        }

        source.AddHook(
            WndProc);

        _source =
            source;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _window.SourceInitialized -=
            HandleSourceInitialized;

        if (_source is not null)
        {
            _source.RemoveHook(
                WndProc);

            _source =
                null;
        }

        _disposed =
            true;
    }

    private void HandleSourceInitialized(
        object? sender,
        EventArgs eventArgs)
    {
        AttachIfReady();
    }

    private IntPtr WndProc(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (
            message == WmSysCommand
            && (
                wParam.ToInt64()
                & 0xFFF0
            ) == ScMinimize
        )
        {
            handled =
                true;

            _window
                .Dispatcher
                .BeginInvoke(
                    _minimizeRequested);

            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private static IntPtr GetWindowLongPtrSafe(
        IntPtr hwnd,
        int index)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(
                hwnd,
                index)
            : new IntPtr(
                GetWindowLong32(
                    hwnd,
                    index));
    }

    private static IntPtr SetWindowLongPtrSafe(
        IntPtr hwnd,
        int index,
        IntPtr value)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(
                hwnd,
                index,
                value)
            : new IntPtr(
                SetWindowLong32(
                    hwnd,
                    index,
                    value.ToInt32()));
    }

    [DllImport(
        "user32.dll",
        EntryPoint =
            "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(
        IntPtr hwnd,
        int index);

    [DllImport(
        "user32.dll",
        EntryPoint =
            "GetWindowLong")]
    private static extern int GetWindowLong32(
        IntPtr hwnd,
        int index);

    [DllImport(
        "user32.dll",
        EntryPoint =
            "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(
        IntPtr hwnd,
        int index,
        IntPtr value);

    [DllImport(
        "user32.dll",
        EntryPoint =
            "SetWindowLong")]
    private static extern int SetWindowLong32(
        IntPtr hwnd,
        int index,
        int value);
}

public sealed record CoreHostPanelShellDiagnosticSnapshot(
    string Action,
    string Reason,
    bool? WasVisible,
    bool IsVisible,
    bool IsActive,
    string FocusedElementType,
    double Left,
    double Top,
    double Width,
    double Height,
    double WorkAreaLeft,
    double WorkAreaTop,
    double WorkAreaWidth,
    double WorkAreaHeight,
    bool Topmost,
    bool ShowActivated,
    bool ShowInTaskbar,
    bool NoActivateExtendedStyle);

public static class CoreHostPanelShellDiagnosticFormatter
{
    public static string Format(
        CoreHostPanelShellDiagnosticSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            snapshot);

        return string.Join(
            "; ",
            new[]
            {
                $"action={snapshot.Action}",
                $"reason={snapshot.Reason}",
                $"wasVisible={FormatNullableBoolean(snapshot.WasVisible)}",
                $"isVisible={snapshot.IsVisible}",
                $"isActive={snapshot.IsActive}",
                $"focusedElementType={snapshot.FocusedElementType}",
                $"left={FormatDouble(snapshot.Left)}",
                $"top={FormatDouble(snapshot.Top)}",
                $"width={FormatDouble(snapshot.Width)}",
                $"height={FormatDouble(snapshot.Height)}",
                $"workAreaLeft={FormatDouble(snapshot.WorkAreaLeft)}",
                $"workAreaTop={FormatDouble(snapshot.WorkAreaTop)}",
                $"workAreaWidth={FormatDouble(snapshot.WorkAreaWidth)}",
                $"workAreaHeight={FormatDouble(snapshot.WorkAreaHeight)}",
                $"topmost={snapshot.Topmost}",
                $"showActivated={snapshot.ShowActivated}",
                $"showInTaskbar={snapshot.ShowInTaskbar}",
                $"noActivateExtendedStyle={snapshot.NoActivateExtendedStyle}"
            });
    }

    private static string FormatNullableBoolean(
        bool? value)
    {
        return value.HasValue
            ? value.Value.ToString()
            : "<unknown>";
    }

    private static string FormatDouble(
        double value)
    {
        return value.ToString(
            "0.##",
            CultureInfo.InvariantCulture);
    }
}
