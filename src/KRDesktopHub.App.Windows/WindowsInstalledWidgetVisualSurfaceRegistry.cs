
using System.Windows;

namespace KRDesktopHub.App.Windows;

public sealed class WindowsInstalledWidgetVisualSurfaceRegistry
{
    private readonly Dictionary<
        string,
        Func<FrameworkElement>> _factories =
            new(
                StringComparer.OrdinalIgnoreCase);

    public void Register(
        string widgetId,
        Func<FrameworkElement> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        ArgumentNullException.ThrowIfNull(
            factory);

        if (!_factories.TryAdd(
            widgetId,
            factory))
        {
            throw new InvalidOperationException(
                $"A Windows Widget visual-surface factory is already registered: {widgetId}");
        }
    }

    public bool Unregister(
        string widgetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        return _factories.Remove(
            widgetId);
    }

    public FrameworkElement? TryCreate(
        string widgetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        return _factories.TryGetValue(
            widgetId,
            out var factory)
                ? factory()
                : null;
    }
}
