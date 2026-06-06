
using KRDesktopHub.Contracts;
using KRDesktopHub.Core;
using KRDesktopHub.Platform.Abstractions;
using KRDesktopHub.Platform.Windows;

namespace KRDesktopHub.App.Windows;

public sealed class WindowsWidgetFrameworkServices
{
    private readonly InMemoryWidgetCapabilityApprovalStore _approvals =
        new();

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

        TrayIcons =
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
    }

    public IWidgetHostLayoutClient HostLayout { get; }

    public IWidgetDialogBroker Dialogs { get; }

    public IWidgetTrayIconBroker TrayIcons { get; }

    public void SynchronizeApprovedCapabilities(
        InstalledWidgetCatalogSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            snapshot);

        foreach (var widget in snapshot.Widgets)
        {
            _approvals.SetApprovedCapabilities(
                widget.WidgetId,
                widget.Capabilities);
        }
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
