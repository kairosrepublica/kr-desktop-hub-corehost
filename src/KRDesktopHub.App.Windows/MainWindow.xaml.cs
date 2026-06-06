
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using KRDesktopHub.Core;

namespace KRDesktopHub.App.Windows;

public partial class MainWindow
    : Window
{
    private bool _allowCloseAndExit;

    public bool CloseButtonHidesToTray { get; set; } =
        true;

    public event EventHandler? CloseExitRequested;

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
    }

    public void RenderInstalledWidgets(
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

        var nextCards =
            visible
                .Select(
                    widget =>
                    {
                        var card =
                            new WidgetHostCard();

                        card.Bind(
                            widget,
                            surfaces.TryCreate(
                                widget.WidgetId));

                        card.CollapseRequested +=
                            (_, request) =>
                                WidgetCollapseRequested?.Invoke(
                                    this,
                                    request);

                        return card;
                    })
                .ToArray();

        WidgetHostSurface.Children.Clear();

        foreach (var card in nextCards)
        {
            WidgetHostSurface.Children.Add(
                card);
        }

        EmptyWidgetHostState.Visibility =
            visible.Length == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

        var desiredHeight =
            visible.Length == 0
                ? 180
                : snapshot.Layout.TotalDesiredHeightDip
                    + 16;

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

    protected override void OnClosing(
        CancelEventArgs e)
    {
        if (!_allowCloseAndExit)
        {
            e.Cancel =
                true;

            if (CloseButtonHidesToTray)
            {
                Hide();
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
