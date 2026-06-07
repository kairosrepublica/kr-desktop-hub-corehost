using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using KRDesktopHub.Contracts;
using KRDesktopHub.Core;

namespace KRDesktopHub.App.Windows;

public partial class MainWindow
    : Window
{
    private readonly Dictionary<
        string,
        WidgetHostCard> _widgetCards =
            new(
                StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<
        string,
        FrameworkElement?> _widgetContents =
            new(
                StringComparer.OrdinalIgnoreCase);

    private bool _allowCloseAndExit;
    private bool _minimizeToTrayRequestPending;
    private CoreHostPanelNativeShellAdapter? _nativeShellAdapter;

    public bool CloseButtonHidesToTray { get; set; } =
        true;

    public event EventHandler? CloseExitRequested;

    public event EventHandler? CloseToTrayRequested;

    public event EventHandler? MinimizeToTrayRequested;

    public event EventHandler? WidgetManagerRequested;

    public event EventHandler? SettingsCenterRequested;

    public event EventHandler? WidgetHostRefreshRequested;

    public event EventHandler<
        WidgetCollapseRequestedEventArgs>? WidgetCollapseRequested;

    public const double DefaultPopupWidthDip =
        600;

    public MainWindow()
    {
        InitializeComponent();

        ShowActivated =
            CoreHostPanelShellPolicy
                .ShowActivated;

        ShowInTaskbar =
            CoreHostPanelShellPolicy
                .ShowInTaskbar;
    }

    public bool NoActivateExtendedStyleApplied =>
        _nativeShellAdapter?
            .NoActivateExtendedStyleApplied
        ?? false;

    public void RenderInstalledWidgets(
        InstalledWidgetCatalogSnapshot snapshot,
        WindowsInstalledWidgetVisualSurfaceRegistry surfaces)
    {
        ReconcileInstalledWidgets(
            snapshot,
            surfaces);
    }

    public void ReconcileInstalledWidgets(
        InstalledWidgetCatalogSnapshot snapshot,
        WindowsInstalledWidgetVisualSurfaceRegistry surfaces)
    {
        ArgumentNullException.ThrowIfNull(
            snapshot);

        ArgumentNullException.ThrowIfNull(
            surfaces);

        var visible =
            snapshot
                .Widgets
                .Where(
                    widget =>
                        widget.Enabled)
                .OrderBy(
                    widget =>
                        widget.Order)
                .ThenBy(
                    widget =>
                        widget.WidgetId,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var visibleWidgetIds =
            visible
                .Select(
                    widget =>
                        widget.WidgetId)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        foreach (var removedWidgetId in
            _widgetCards
                .Keys
                .Where(
                    widgetId =>
                        !visibleWidgetIds.Contains(
                            widgetId))
                .ToArray())
        {
            _widgetCards.Remove(
                removedWidgetId);

            _widgetContents.Remove(
                removedWidgetId);
        }

        foreach (var widget in visible)
        {
            if (!_widgetCards.TryGetValue(
                widget.WidgetId,
                out var card))
            {
                card =
                    new WidgetHostCard();

                card.CollapseRequested +=
                    (_, request) =>
                        WidgetCollapseRequested?.Invoke(
                            this,
                            request);

                _widgetCards[widget.WidgetId] =
                    card;
            }

            if (!_widgetContents.TryGetValue(
                widget.WidgetId,
                out var content))
            {
                content =
                    surfaces.TryCreate(
                        widget.WidgetId);

                _widgetContents[widget.WidgetId] =
                    content;
            }

            card.Bind(
                widget,
                content);
        }

        WidgetHostSurface.Children.Clear();

        foreach (var widget in visible)
        {
            WidgetHostSurface.Children.Add(
                _widgetCards[widget.WidgetId]);
        }

        EmptyWidgetHostState.Visibility =
            visible.Length == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

        ApplyWidgetHostLayout(
            snapshot.Layout);
    }

    public void ApplyWidgetHostLayout(
        WidgetHostLayoutSnapshot layout)
    {
        ArgumentNullException.ThrowIfNull(
            layout);

        var desiredHeight =
            layout
                .Widgets
                .Any(
                    widget =>
                        widget.Enabled)
                ? layout.TotalDesiredHeightDip
                    + 16
                : 180;

        ApplyWidgetHostDesiredHeight(
            desiredHeight);
    }

    public void ApplyWidgetHostDesiredHeight(
        double desiredHeightDip)
    {
        if (
            !double.IsFinite(
                desiredHeightDip)
            || desiredHeightDip <= 0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(desiredHeightDip));
        }

        var observedHostHeightDip =
            double.IsFinite(
                ActualHeight)
            && ActualHeight > 0
                ? ActualHeight
                : Height;

        var viewport =
            WidgetHostViewportHeightPolicy
                .PreserveOwnerSizedViewport(
                    observedHostHeightDip,
                    desiredHeightDip);

        WidgetHostScrollViewer.VerticalScrollBarVisibility =
            viewport.HostLevelScrollingRequired
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled;
    }

    public void AllowCloseAndExit()
    {
        _allowCloseAndExit =
            true;
    }

    private void OpenSettingsCenterButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SettingsCenterRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    private void OpenWidgetManagerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        WidgetManagerRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    private void RefreshWidgetHostButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        WidgetHostRefreshRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    protected override void OnSourceInitialized(
        EventArgs e)
    {
        base.OnSourceInitialized(
            e);

        _nativeShellAdapter ??=
            new CoreHostPanelNativeShellAdapter(
                this,
                RequestMinimizeToTray);

        _nativeShellAdapter.AttachIfReady();
    }

    protected override void OnStateChanged(
        EventArgs e)
    {
        base.OnStateChanged(
            e);

        if (
            CoreHostPanelShellPolicy.HideOnMinimize
            && WindowState == WindowState.Minimized
        )
        {
            RequestMinimizeToTray();
        }
    }

    protected override void OnClosed(
        EventArgs e)
    {
        _nativeShellAdapter?.Dispose();

        _nativeShellAdapter =
            null;

        base.OnClosed(
            e);
    }

    private void RequestMinimizeToTray()
    {
        if (_minimizeToTrayRequestPending)
        {
            return;
        }

        _minimizeToTrayRequestPending =
            true;

        _ =
            Dispatcher.BeginInvoke(
                new Action(
                    () =>
                    {
                        try
                        {
                            MinimizeToTrayRequested?.Invoke(
                                this,
                                EventArgs.Empty);
                        }
                        finally
                        {
                            _minimizeToTrayRequestPending =
                                false;
                        }
                    }));
    }

    protected override void OnClosing(
        CancelEventArgs e)
    {
        if (!_allowCloseAndExit)
        {
            e.Cancel =
                true;

            if (CloseButtonHidesToTray)
            {
                CloseToTrayRequested?.Invoke(
                    this,
                    EventArgs.Empty);
            }
            else
            {
                CloseExitRequested?.Invoke(
                    this,
                    EventArgs.Empty);
            }

            return;
        }

        base.OnClosing(
            e);
    }
}
