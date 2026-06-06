using KRDesktopHub.Core;

namespace KRDesktopHub.App.Windows;

public sealed class WidgetHostOperationFailedEventArgs
    : EventArgs
{
    public WidgetHostOperationFailedEventArgs(
        string operation,
        Exception exception)
    {
        Operation =
            operation;

        Exception =
            exception
            ?? throw new ArgumentNullException(
                nameof(exception));
    }

    public string Operation { get; }

    public Exception Exception { get; }
}

public sealed class InstalledWidgetHostCompositionCoordinator
{
    private readonly MainWindow _panel;
    private readonly InternalWidgetManagerService _manager;
    private readonly WindowsInstalledWidgetVisualSurfaceRegistry _surfaces;
    private readonly WindowsWidgetFrameworkServices _frameworkServices;
    private readonly WidgetHostOperationSerialQueue _hostOperationQueue =
        new();

    public InstalledWidgetHostCompositionCoordinator(
        MainWindow panel,
        InternalWidgetManagerService manager,
        WindowsInstalledWidgetVisualSurfaceRegistry surfaces,
        WindowsWidgetFrameworkServices frameworkServices)
    {
        _panel =
            panel
            ?? throw new ArgumentNullException(
                nameof(panel));

        _manager =
            manager
            ?? throw new ArgumentNullException(
                nameof(manager));

        _surfaces =
            surfaces
            ?? throw new ArgumentNullException(
                nameof(surfaces));

        _frameworkServices =
            frameworkServices
            ?? throw new ArgumentNullException(
                nameof(frameworkServices));

        _panel.WidgetCollapseRequested +=
            (_, request) =>
                _ = ObserveOperationAsync(
                    "Widget-card collapse or expand",
                    cancellationToken =>
                        SetCollapsedAsync(
                            request.WidgetId,
                            request.Collapsed,
                            cancellationToken));

        _panel.WidgetHostRefreshRequested +=
            (_, _) =>
                _ = ObserveOperationAsync(
                    "Widget-host refresh",
                    RefreshAsync);
    }

    public event EventHandler<
        WidgetHostOperationFailedEventArgs>? OperationFailed;

    public InstalledWidgetCatalogSnapshot? LastSnapshot { get; private set; }

    public Task<InstalledWidgetCatalogSnapshot> RefreshAsync(
        CancellationToken cancellationToken)
    {
        return _hostOperationQueue
            .RunAsync(
                RefreshCoreAsync,
                cancellationToken);
    }

    public Task<InstalledWidgetCatalogSnapshot> SetEnabledAsync(
        string widgetId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        return _hostOperationQueue
            .RunAsync(
                async innerCancellationToken =>
                {
                    _ =
                        _manager
                            .SetInstalledWidgetEnabled(
                                widgetId,
                                enabled);

                    return await RefreshCoreAsync(
                            innerCancellationToken)
                        .ConfigureAwait(
                            true);
                },
                cancellationToken);
    }

    public Task<InstalledWidgetCatalogSnapshot> SetCollapsedAsync(
        string widgetId,
        bool collapsed,
        CancellationToken cancellationToken)
    {
        return _hostOperationQueue
            .RunAsync(
                async innerCancellationToken =>
                {
                    _ =
                        _manager
                            .SetInstalledWidgetCollapsed(
                                widgetId,
                                collapsed);

                    return await RefreshCoreAsync(
                            innerCancellationToken)
                        .ConfigureAwait(
                            true);
                },
                cancellationToken);
    }

    public Task<InstalledWidgetCatalogSnapshot> MoveAsync(
        string widgetId,
        int direction,
        CancellationToken cancellationToken)
    {
        if (direction is not -1
            and not 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction));
        }

        return _hostOperationQueue
            .RunAsync(
                async innerCancellationToken =>
                {
                    var snapshot =
                        await _manager
                            .RefreshInstalledWidgetsAsync(
                                innerCancellationToken)
                            .ConfigureAwait(
                                true);

                    var ordered =
                        snapshot
                            .Widgets
                            .OrderBy(
                                widget =>
                                    widget.Order)
                            .ThenBy(
                                widget =>
                                    widget.WidgetId,
                                StringComparer.OrdinalIgnoreCase)
                            .ToArray();

                    var index =
                        Array.FindIndex(
                            ordered,
                            widget =>
                                string.Equals(
                                    widget.WidgetId,
                                    widgetId,
                                    StringComparison.OrdinalIgnoreCase));

                    var targetIndex =
                        index
                        + direction;

                    if (index < 0
                        || targetIndex < 0
                        || targetIndex >= ordered.Length)
                    {
                        return await RefreshCoreAsync(
                                innerCancellationToken)
                            .ConfigureAwait(
                                true);
                    }

                    _ =
                        _manager
                            .SetInstalledWidgetOrder(
                                ordered[index].WidgetId,
                                ordered[targetIndex].Order);

                    _ =
                        _manager
                            .SetInstalledWidgetOrder(
                                ordered[targetIndex].WidgetId,
                                ordered[index].Order);

                    return await RefreshCoreAsync(
                            innerCancellationToken)
                        .ConfigureAwait(
                            true);
                },
                cancellationToken);
    }

    private async Task<InstalledWidgetCatalogSnapshot> RefreshCoreAsync(
        CancellationToken cancellationToken)
    {
        var candidate =
            await _manager
                .RefreshInstalledWidgetsAsync(
                    cancellationToken)
                .ConfigureAwait(
                    true);

        if (!WidgetHostCatalogRefreshAcceptancePolicy
            .ShouldApply(
                LastSnapshot,
                candidate))
        {
            throw new InvalidOperationException(
                "Widget-host refresh rejected a degraded installed catalog snapshot so the last known-good panel remains visible. Discovery failures: "
                + string.Join(
                    " | ",
                    candidate
                        .Failures
                        .Select(
                            failure =>
                                failure.InstalledPath
                                + ": "
                                + failure.Error)));
        }

        _frameworkServices
            .SynchronizeApprovedCapabilities(
                candidate);

        _panel.RenderInstalledWidgets(
            candidate,
            _surfaces);

        LastSnapshot =
            candidate;

        return candidate;
    }

    private async Task ObserveOperationAsync(
        string operation,
        Func<CancellationToken, Task<InstalledWidgetCatalogSnapshot>> action)
    {
        try
        {
            _ =
                await action(
                        CancellationToken.None)
                    .ConfigureAwait(
                        true);
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(
                this,
                new WidgetHostOperationFailedEventArgs(
                    operation,
                    exception));
        }
    }
}
