using KRDesktopHub.Contracts;

namespace KRDesktopHub.Core;

public sealed record WidgetHostChromePresentation(
    bool Collapsed,
    string StatusLabel,
    string ToggleActionLabel)
{
    public static WidgetHostChromePresentation FromCollapsed(
        bool collapsed)
    {
        return collapsed
            ? new WidgetHostChromePresentation(
                Collapsed:
                    true,
                StatusLabel:
                    "Collapsed",
                ToggleActionLabel:
                    "Expand")
            : new WidgetHostChromePresentation(
                Collapsed:
                    false,
                StatusLabel:
                    "Expanded",
                ToggleActionLabel:
                    "Collapse");
    }
}

public sealed class WidgetHostChromeTransitionController
{
    private readonly WidgetHostLayoutController _layoutController;

    private readonly WidgetHostOperationSerialQueue _transitionQueue =
        new();

    public WidgetHostChromeTransitionController(
        WidgetHostLayoutController layoutController)
    {
        _layoutController =
            layoutController
            ?? throw new ArgumentNullException(
                nameof(layoutController));
    }

    public Task<WidgetHostLayoutSnapshot> ToggleCollapsedAsync(
        string widgetId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        return _transitionQueue
            .RunAsync(
                innerCancellationToken =>
                {
                    innerCancellationToken
                        .ThrowIfCancellationRequested();

                    var current =
                        _layoutController
                            .GetLayout()
                            .Widgets
                            .SingleOrDefault(
                                widget =>
                                    string.Equals(
                                        widget.WidgetId,
                                        widgetId,
                                        StringComparison.OrdinalIgnoreCase))
                        ?? throw new KeyNotFoundException(
                            $"Widget host registration was not found: {widgetId}");

                    return Task.FromResult(
                        _layoutController
                            .SetCollapsed(
                                widgetId,
                                collapsed:
                                    !current.Collapsed));
                },
                cancellationToken);
    }

    public Task<WidgetHostLayoutSnapshot> SetCollapsedAsync(
        string widgetId,
        bool collapsed,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        return _transitionQueue
            .RunAsync(
                innerCancellationToken =>
                {
                    innerCancellationToken
                        .ThrowIfCancellationRequested();

                    return Task.FromResult(
                        _layoutController
                            .SetCollapsed(
                                widgetId,
                                collapsed));
                },
                cancellationToken);
    }
}
