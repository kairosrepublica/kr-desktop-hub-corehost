using System.Windows;
using System.Windows.Controls;
using KRDesktopHub.Core;

namespace KRDesktopHub.App.Windows;

public sealed class WidgetCollapseRequestedEventArgs
    : EventArgs
{
    public WidgetCollapseRequestedEventArgs(
        string widgetId)
    {
        WidgetId =
            widgetId;
    }

    public string WidgetId { get; }
}

public partial class WidgetHostCard
    : UserControl
{
    private InstalledWidgetCatalogItem? _widget;

    public WidgetHostCard()
    {
        InitializeComponent();
    }

    public event EventHandler<
        WidgetCollapseRequestedEventArgs>? CollapseRequested;

    public void Bind(
        InstalledWidgetCatalogItem widget,
        FrameworkElement? content)
    {
        ArgumentNullException.ThrowIfNull(
            widget);

        _widget =
            widget;

        var chrome =
            WidgetHostChromePresentation
                .FromCollapsed(
                    widget.Collapsed);

        Height =
            widget.ActualHeightDip;

        MinHeight =
            widget.MinimumCollapsedHeightDip;

        TitleTextBlock.Text =
            widget.DisplayName;

        MetadataTextBlock.Text =
            widget.WidgetId
            + " | v"
            + widget.PackageVersion
            + " | "
            + chrome.StatusLabel;

        CollapseButton.Content =
            chrome.ToggleActionLabel;

        WidgetContentPresenter.Visibility =
            chrome.Collapsed
                ? Visibility.Collapsed
                : Visibility.Visible;

        WidgetContentPresenter.Content =
            content
            ?? WidgetContentPresenter.Content
            ?? CreateFallbackContent(
                widget);
    }

    private void CollapseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_widget is null)
        {
            return;
        }

        CollapseRequested?.Invoke(
            this,
            new WidgetCollapseRequestedEventArgs(
                _widget.WidgetId));
    }

    private static FrameworkElement CreateFallbackContent(
        InstalledWidgetCatalogItem widget)
    {
        return new Border
        {
            Background =
                System.Windows.Media.Brushes.Transparent,

            Child =
                new TextBlock
                {
                    Text =
                        "Installed Widget surface is reserved. "
                        + "The isolated Widget package will provide its production visual content.",

                    TextWrapping =
                        TextWrapping.Wrap,

                    Foreground =
                        System.Windows.Media.Brushes.DimGray,

                    FontSize =
                        11
                }
        };
    }
}
