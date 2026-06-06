using KRDesktopHub.Contracts;

namespace KRDesktopHub.Core;

public static class InstalledWidgetCatalogProjection
{
    public static InstalledWidgetCatalogSnapshot ApplyLayout(
        InstalledWidgetCatalogSnapshot acceptedSnapshot,
        WidgetHostLayoutSnapshot layout)
    {
        ArgumentNullException.ThrowIfNull(
            acceptedSnapshot);

        ArgumentNullException.ThrowIfNull(
            layout);

        var layoutByWidgetId =
            layout
                .Widgets
                .ToDictionary(
                    widget =>
                        widget.WidgetId,
                    StringComparer.OrdinalIgnoreCase);

        var widgets =
            acceptedSnapshot
                .Widgets
                .Select(
                    widget =>
                    {
                        if (!layoutByWidgetId.TryGetValue(
                            widget.WidgetId,
                            out var surface))
                        {
                            throw new InvalidOperationException(
                                $"Accepted Widget is missing from the framework layout: {widget.WidgetId}");
                        }

                        return widget with
                        {
                            Enabled =
                                surface.Enabled,
                            Collapsed =
                                surface.Collapsed,
                            Order =
                                surface.Order,
                            PreferredExpandedHeightDip =
                                surface.PreferredExpandedHeightDip,
                            MinimumCollapsedHeightDip =
                                surface.MinimumCollapsedHeightDip,
                            MeasuredDesiredHeightDip =
                                surface.MeasuredDesiredHeightDip,
                            ActualHeightDip =
                                surface.ActualHeightDip
                        };
                    })
                .OrderBy(
                    widget =>
                        widget.Order)
                .ThenBy(
                    widget =>
                        widget.WidgetId,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var acceptedWidgetIds =
            widgets
                .Select(
                    widget =>
                        widget.WidgetId)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        var surfaces =
            layout
                .Widgets
                .Where(
                    widget =>
                        acceptedWidgetIds.Contains(
                            widget.WidgetId))
                .OrderBy(
                    widget =>
                        widget.Order)
                .ThenBy(
                    widget =>
                        widget.WidgetId,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var visible =
            surfaces
                .Where(
                    widget =>
                        widget.Enabled)
                .ToArray();

        var totalDesiredHeightDip =
            visible.Sum(
                widget =>
                    widget.ActualHeightDip)
            + Math.Max(
                0,
                visible.Length - 1)
                * WidgetHostFrameworkDefaults
                    .DefaultWidgetGapDip;

        var scrollingRequired =
            layout.HostLevelScrollingRequired
            && totalDesiredHeightDip
                > layout.EffectiveViewportHeightDip;

        var projectedLayout =
            new WidgetHostLayoutSnapshot(
                layout.HostWidthDip,
                totalDesiredHeightDip,
                scrollingRequired
                    ? layout.EffectiveViewportHeightDip
                    : totalDesiredHeightDip,
                scrollingRequired,
                surfaces);

        return new InstalledWidgetCatalogSnapshot(
            widgets,
            acceptedSnapshot.Failures,
            projectedLayout);
    }
}
