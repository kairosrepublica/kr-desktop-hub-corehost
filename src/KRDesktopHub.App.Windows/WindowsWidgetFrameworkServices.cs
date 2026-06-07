
using KRDesktopHub.Contracts;
using KRDesktopHub.Core;
using KRDesktopHub.Platform.Abstractions;
using KRDesktopHub.Platform.Windows;

namespace KRDesktopHub.App.Windows;

public sealed class WindowsWidgetFrameworkServices
{
    private readonly InMemoryWidgetCapabilityApprovalStore _approvals =
        new();

    private readonly GovernedWidgetTrayIconBroker _trayIcons;

    public WindowsWidgetFrameworkServices(
        MainWindow panel,
        WindowsTrayService tray,
        WidgetHostLayoutController layoutController)
    {
        ArgumentNullException.ThrowIfNull(
            panel);

        ArgumentNullException.ThrowIfNull(
            tray);

        ArgumentNullException.ThrowIfNull(
            layoutController);

        var authorizer =
            new DefaultWidgetCapabilityAuthorizer(
                _approvals,
                new InMemoryWidgetCapabilityAuditSink());

        HostLayout =
            new GovernedWidgetHostLayoutClient(
                authorizer,
                layoutController);

        Dialogs =
            new GovernedWidgetDialogBroker(
                authorizer,
                new WindowsWidgetDialogPresenter(
                    panel)
                    .PresentAsync);

        _trayIcons =
            new GovernedWidgetTrayIconBroker(
                authorizer,
                new[]
                {
                    new WidgetTrayIconStateDefinition(
                        WindowsTrayVisualStateCatalog.Default,
                        0,
                        "Default CoreHost tray state."),

                    new WidgetTrayIconStateDefinition(
                        WindowsTrayVisualStateCatalog.Information,
                        1000,
                        "Approved informational tray state."),

                    new WidgetTrayIconStateDefinition(
                        WindowsTrayVisualStateCatalog.Warning,
                        1000,
                        "Approved warning tray state."),

                    new WidgetTrayIconStateDefinition(
                        WindowsTrayVisualStateCatalog.Error,
                        1000,
                        "Approved error tray state."),

                    new WidgetTrayIconStateDefinition(
                        WindowsTrayVisualStateCatalog.Shield,
                        1000,
                        "Approved shield tray state.")
                },
                WindowsTrayVisualStateCatalog.Default,
                async (selection, cancellationToken) =>
                    await tray.SetStatusAsync(
                        new TrayStatus(
                            CoreHostTrayStatusText
                                .FromWidgetSelection(
                                    selection),
                            VisualState:
                                selection.IconStateKey),
                        cancellationToken));

        TrayIcons =
            _trayIcons;
    }

    public IWidgetHostLayoutClient HostLayout { get; }

    public IWidgetDialogBroker Dialogs { get; }

    public IWidgetTrayIconBroker TrayIcons { get; }

    public async Task SynchronizeApprovedCapabilitiesAsync(
        InstalledWidgetCatalogSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            snapshot);

        cancellationToken.ThrowIfCancellationRequested();

        var capabilitiesByWidgetId =
            snapshot
                .Widgets
                .ToDictionary(
                    widget =>
                        widget.WidgetId,
                    widget =>
                        widget.Capabilities,
                    StringComparer.OrdinalIgnoreCase);

        _approvals.ReconcileApprovedCapabilities(
            capabilitiesByWidgetId);

        var approvedTrayWidgetIds =
            snapshot
                .Widgets
                .Where(
                    widget =>
                        widget.Capabilities.Contains(
                            WidgetCapabilityIds.TrayIconRequest,
                            StringComparer.OrdinalIgnoreCase))
                .Select(
                    widget =>
                        widget.WidgetId)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        _ =
            await _trayIcons
                .RevokeRequestsExceptAsync(
                    approvedTrayWidgetIds,
                    cancellationToken)
                .ConfigureAwait(
                    true);
    }

    public IntegratedWidgetContext CreateIntegratedContext(
        IWidgetContext inner)
    {
        return new IntegratedWidgetContext(
            inner,
            HostLayout,
            Dialogs,
            TrayIcons);
    }
}
