using KRDesktopHub.Contracts;
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
    private readonly WidgetHostChromeTransitionController _chromeTransitions;

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

        _chromeTransitions =
            new WidgetHostChromeTransitionController(
                _manager
                    .InstalledCatalog
                    .LayoutController);

        _manager
            .InstalledCatalog
            .LayoutController
            .LayoutChanged +=
                HandleLayoutChanged;

        _panel.WidgetCollapseRequested +=
            (_, request) =>
                _ = ObserveOperationAsync(
                    "Widget-card collapse or expand",
                    cancellationToken =>
                        ToggleCollapsedAsync(
                            request.WidgetId,
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

    public Task<InstalledWidgetCatalogSnapshot> SynchronizeStateAsync(
        CancellationToken cancellationToken)
    {
        return _hostOperationQueue
            .RunAsync(
                innerCancellationToken =>
                {
                    innerCancellationToken
                        .ThrowIfCancellationRequested();

                    return Task.FromResult(
                        ApplyStateOnlyLayout(
                            _manager
                                .GetInstalledWidgetLayout()));
                },
                cancellationToken);
    }

    public Task<InstalledWidgetCatalogSnapshot> SetEnabledAsync(
        string widgetId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        return _hostOperationQueue
            .RunAsync(
                innerCancellationToken =>
                {
                    innerCancellationToken
                        .ThrowIfCancellationRequested();

                    return Task.FromResult(
                        ApplyStateOnlyLayout(
                            _manager
                                .SetInstalledWidgetEnabled(
                                    widgetId,
                                    enabled)));
                },
                cancellationToken);
    }

    public Task<InstalledWidgetCatalogSnapshot> ToggleCollapsedAsync(
        string widgetId,
        CancellationToken cancellationToken)
    {
        return _hostOperationQueue
            .RunAsync(
                async innerCancellationToken =>
                {
                    var layout =
                        await _chromeTransitions
                            .ToggleCollapsedAsync(
                                widgetId,
                                innerCancellationToken)
                            .ConfigureAwait(
                                true);

                    return ApplyStateOnlyLayout(
                        layout);
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
                    var layout =
                        await _chromeTransitions
                            .SetCollapsedAsync(
                                widgetId,
                                collapsed,
                                innerCancellationToken)
                            .ConfigureAwait(
                                true);

                    return ApplyStateOnlyLayout(
                        layout);
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
                innerCancellationToken =>
                {
                    innerCancellationToken
                        .ThrowIfCancellationRequested();

                    var snapshot =
                        GetRequiredSnapshot();

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
                        return Task.FromResult(
                            snapshot);
                    }

                    _ =
                        _manager
                            .SetInstalledWidgetOrder(
                                ordered[index].WidgetId,
                                ordered[targetIndex].Order);

                    var layout =
                        _manager
                            .SetInstalledWidgetOrder(
                                ordered[targetIndex].WidgetId,
                                ordered[index].Order);

                    return Task.FromResult(
                        ApplyStateOnlyLayout(
                            layout));
                },
                cancellationToken);
    }

    private async Task<InstalledWidgetCatalogSnapshot> RefreshCoreAsync(
        CancellationToken cancellationToken)
    {
        var candidate =
            await _manager
                .DiscoverInstalledWidgetsAsync(
                    cancellationToken)
                .ConfigureAwait(
                    true);

        if (!WidgetHostCatalogRefreshAcceptancePolicy
            .ShouldApply(
                LastSnapshot,
                candidate))
        {
            throw new InvalidOperationException(
                "Widget-host refresh rejected a degraded installed catalog candidate before any internal state mutation so the complete last-known-good host remains active. Discovery failures: "
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

        var committed =
            _manager
                .CommitAcceptedInstalledWidgets(
                    candidate);

        await _frameworkServices
            .SynchronizeApprovedCapabilitiesAsync(
                committed,
                cancellationToken)
            .ConfigureAwait(
                true);

        _panel.ReconcileInstalledWidgets(
            committed,
            _surfaces);

        LastSnapshot =
            committed;

        return committed;
    }

    private void HandleLayoutChanged(
        WidgetHostLayoutSnapshot layout)
    {
        ArgumentNullException.ThrowIfNull(
            layout);

        if (_panel.Dispatcher.CheckAccess())
        {
            _panel.ApplyWidgetHostLayout(
                layout);

            return;
        }

        _ =
            _panel
                .Dispatcher
                .InvokeAsync(
                    () =>
                        _panel.ApplyWidgetHostLayout(
                            layout));
    }

    private InstalledWidgetCatalogSnapshot ApplyStateOnlyLayout(
        WidgetHostLayoutSnapshot layout)
    {
        var snapshot =
            InstalledWidgetCatalogProjection
                .ApplyLayout(
                    GetRequiredSnapshot(),
                    layout);

        _panel.ReconcileInstalledWidgets(
            snapshot,
            _surfaces);

        LastSnapshot =
            snapshot;

        return snapshot;
    }

    private InstalledWidgetCatalogSnapshot GetRequiredSnapshot()
    {
        return LastSnapshot
            ?? throw new InvalidOperationException(
                "Widget-host state cannot be mutated before the initial installed catalog refresh has completed.");
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
