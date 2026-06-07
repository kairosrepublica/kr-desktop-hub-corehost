using System.Globalization;

namespace KRDesktopHub.App.Windows;

public static class CoreHostPanelShellPolicy
{
    public const bool ShowActivated =
        false;

    public const bool ShowInTaskbar =
        false;

    public const bool ForceActivateAfterOrdinaryShow =
        false;
}

public sealed record CoreHostPanelShellDiagnosticSnapshot(
    string Action,
    string Reason,
    bool? WasVisible,
    bool IsVisible,
    bool IsActive,
    string FocusedElementType,
    double Left,
    double Top,
    double Width,
    double Height,
    double WorkAreaLeft,
    double WorkAreaTop,
    double WorkAreaWidth,
    double WorkAreaHeight,
    bool Topmost,
    bool ShowActivated,
    bool ShowInTaskbar);

public static class CoreHostPanelShellDiagnosticFormatter
{
    public static string Format(
        CoreHostPanelShellDiagnosticSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            snapshot);

        return string.Join(
            "; ",
            new[]
            {
                $"action={snapshot.Action}",
                $"reason={snapshot.Reason}",
                $"wasVisible={FormatNullableBoolean(snapshot.WasVisible)}",
                $"isVisible={snapshot.IsVisible}",
                $"isActive={snapshot.IsActive}",
                $"focusedElementType={snapshot.FocusedElementType}",
                $"left={FormatDouble(snapshot.Left)}",
                $"top={FormatDouble(snapshot.Top)}",
                $"width={FormatDouble(snapshot.Width)}",
                $"height={FormatDouble(snapshot.Height)}",
                $"workAreaLeft={FormatDouble(snapshot.WorkAreaLeft)}",
                $"workAreaTop={FormatDouble(snapshot.WorkAreaTop)}",
                $"workAreaWidth={FormatDouble(snapshot.WorkAreaWidth)}",
                $"workAreaHeight={FormatDouble(snapshot.WorkAreaHeight)}",
                $"topmost={snapshot.Topmost}",
                $"showActivated={snapshot.ShowActivated}",
                $"showInTaskbar={snapshot.ShowInTaskbar}"
            });
    }

    private static string FormatNullableBoolean(
        bool? value)
    {
        return value.HasValue
            ? value.Value.ToString()
            : "<unknown>";
    }

    private static string FormatDouble(
        double value)
    {
        return value.ToString(
            "0.##",
            CultureInfo.InvariantCulture);
    }
}
