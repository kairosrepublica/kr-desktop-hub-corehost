using KRDesktopHub.Contracts;

namespace KRDesktopHub.Platform.Windows;

public static class CoreHostTrayStatusText
{
    public const string Ready =
        "KR Desktop Hub - Ready";

    public static string FromWidgetSelection(
        WidgetTrayIconSelection selection)
    {
        ArgumentNullException.ThrowIfNull(
            selection);

        return string.IsNullOrWhiteSpace(
            selection.WidgetId)
                ? Ready
                : "KR Desktop Hub - "
                    + selection.WidgetId
                    + " - "
                    + selection.IconStateKey;
    }
}
