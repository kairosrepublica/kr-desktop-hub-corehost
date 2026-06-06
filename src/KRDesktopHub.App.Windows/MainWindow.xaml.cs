
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

        WidgetHostSurface.Children.Clear();

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

        foreach (var widget in visible)
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
            desiredHeight,
            Math.Max(
                240,
                SystemParameters.WorkArea.Height
                    * 0.92));
    }

    public void ApplyWidgetHostDesiredHeight(
        double desiredHeightDip,
        double maximumWorkAreaHeightDip)
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

        if (
            !double.IsFinite(
                maximumWorkAreaHeightDip)
            || maximumWorkAreaHeightDip <= 0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumWorkAreaHeightDip));
        }

        Width =
            DefaultPopupWidthDip;

        Height =
            Math.Min(
                desiredHeightDip,
                maximumWorkAreaHeightDip);

        WidgetHostScrollViewer.VerticalScrollBarVisibility =
            desiredHeightDip > maximumWorkAreaHeightDip
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
