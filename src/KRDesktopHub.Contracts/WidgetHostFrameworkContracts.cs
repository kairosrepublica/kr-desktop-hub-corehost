
namespace KRDesktopHub.Contracts;

public sealed record WidgetPresentationMetadata(
    bool DefaultEnabled,
    bool DefaultCollapsed,
    double PreferredExpandedHeightDip,
    double MinimumCollapsedHeightDip,
    int SettingsSchemaVersion,
    int StateSchemaVersion);

public sealed record WidgetHostRegistration(
    string WidgetId,
    string DisplayName,
    WidgetPresentationMetadata Presentation,
    int Order);

public sealed record WidgetHostSurfaceSnapshot(
    string WidgetId,
    string DisplayName,
    bool Enabled,
    bool Collapsed,
    int Order,
    double PreferredExpandedHeightDip,
    double MinimumCollapsedHeightDip,
    double MeasuredDesiredHeightDip,
    double ActualHeightDip);

public sealed record WidgetHostLayoutSnapshot(
    double HostWidthDip,
    double TotalDesiredHeightDip,
    double EffectiveViewportHeightDip,
    bool HostLevelScrollingRequired,
    IReadOnlyList<WidgetHostSurfaceSnapshot> Widgets);

public interface IWidgetHostLayoutClient
{
    Task<WidgetHostLayoutSnapshot> ReportDesiredHeightAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        double desiredHeightDip,
        double maximumViewportHeightDip,
        CancellationToken cancellationToken);
}

public sealed record WidgetDialogOption(
    string OptionId,
    string DisplayName,
    string? Description = null);

public sealed record WidgetDialogRequest(
    string DialogId,
    string Title,
    string? Message,
    IReadOnlyList<WidgetDialogOption> Options,
    string? SearchPlaceholder = null);

public sealed record WidgetDialogResult(
    bool Accepted,
    string? SelectedOptionId);

public interface IWidgetDialogBroker
{
    Task<WidgetDialogResult> RequestAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        WidgetDialogRequest request,
        CancellationToken cancellationToken);
}

public sealed record WidgetTrayIconRequest(
    string RequestId,
    string IconStateKey,
    int Priority,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string Reason);

public sealed record WidgetTrayIconSelection(
    string? RequestId,
    string? WidgetId,
    string IconStateKey,
    int Priority,
    DateTimeOffset AppliedAtUtc,
    string Reason);

public interface IWidgetTrayIconBroker
{
    Task<WidgetTrayIconSelection> SubmitAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        WidgetTrayIconRequest request,
        CancellationToken cancellationToken);

    Task<WidgetTrayIconSelection> WithdrawAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        string requestId,
        CancellationToken cancellationToken);

    WidgetTrayIconSelection GetCurrent();
}

public interface IWidgetHostIntegrationContext
    : IWidgetContext
{
    IWidgetHostLayoutClient HostLayout { get; }

    IWidgetDialogBroker Dialogs { get; }

    IWidgetTrayIconBroker TrayIcons { get; }
}
