
using KRDesktopHub.Core;

namespace KRDesktopHub.App.Windows;

public sealed class InstalledWidgetHostCompositionCoordinator
{
    private readonly MainWindow _panel;
    private readonly InternalWidgetManagerService _manager;
    private readonly WindowsInstalledWidgetVisualSurfaceRegistry _surfaces;
    private readonly WindowsWidgetFrameworkServices _frameworkServices;

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
            async (_, request) =>
                await SetCollapsedAsync(
                    request.WidgetId,
                    request.Collapsed,
                    CancellationToken.None);

        _panel.WidgetHostRefreshRequested +=
            async (_, _) =>
                await RefreshAsync(
                    CancellationToken.None);
    }

    public InstalledWidgetCatalogSnapshot? LastSnapshot { get; private set; }

    public async Task<InstalledWidgetCatalogSnapshot> RefreshAsync(
        CancellationToken cancellationToken)
    {
        var snapshot =
            await _manager
                .RefreshInstalledWidgetsAsync(
                    cancellationToken);

        _frameworkServices
            .SynchronizeApprovedCapabilities(
                snapshot);

        LastSnapshot =
            snapshot;

        _panel.RenderInstalledWidgets(
            snapshot,
            _surfaces);

        return snapshot;
    }

    public async Task<InstalledWidgetCatalogSnapshot> SetEnabledAsync(
        string widgetId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        _ =
            _manager
                .SetInstalledWidgetEnabled(
                    widgetId,
                    enabled);

        return await RefreshAsync(
            cancellationToken);
    }

    public async Task<InstalledWidgetCatalogSnapshot> SetCollapsedAsync(
        string widgetId,
        bool collapsed,
        CancellationToken cancellationToken)
    {
        _ =
            _manager
                .SetInstalledWidgetCollapsed(
                    widgetId,
                    collapsed);

        return await RefreshAsync(
            cancellationToken);
    }

    public async Task<InstalledWidgetCatalogSnapshot> MoveAsync(
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

        var snapshot =
            await _manager
                .RefreshInstalledWidgetsAsync(
                    cancellationToken);

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
            return snapshot;
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

        return await RefreshAsync(
            cancellationToken);
    }
}
