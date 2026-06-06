
using KRDesktopHub.Contracts;
using KRDesktopHub.Core;

var tempRoot =
    Path.Combine(
        Path.GetTempPath(),
        "KRDesktopHub-WidgetFramework-"
        + Guid.NewGuid().ToString("N"));

Directory.CreateDirectory(
    tempRoot);

try
{
    var statePath =
        Path.Combine(
            tempRoot,
            "widget-host-state.json");

    var controller =
        new WidgetHostLayoutController(
            new JsonWidgetHostStateStore(
                statePath));

    var presentationWorld =
        new WidgetPresentationMetadata(
            DefaultEnabled:
                true,
            DefaultCollapsed:
                false,
            PreferredExpandedHeightDip:
                220,
            MinimumCollapsedHeightDip:
                44,
            SettingsSchemaVersion:
                1,
            StateSchemaVersion:
                1);

    var presentationTrading =
        new WidgetPresentationMetadata(
            DefaultEnabled:
                true,
            DefaultCollapsed:
                false,
            PreferredExpandedHeightDip:
                500,
            MinimumCollapsedHeightDip:
                44,
            SettingsSchemaVersion:
                1,
            StateSchemaVersion:
                1);

    controller.RegisterOrUpdate(
        new WidgetHostRegistration(
            "kr.world-time-space",
            "KR World Time-Space",
            presentationWorld,
            10));

    var initial =
        controller.RegisterOrUpdate(
            new WidgetHostRegistration(
                "kr.trading-clock",
                "KR Trading Clock",
                presentationTrading,
                20));

    if (
        initial.HostWidthDip != 600
        || initial.TotalDesiredHeightDip != 728
        || initial.HostLevelScrollingRequired
    )
    {
        throw new InvalidOperationException(
            "Default adaptive layout validation failed.");
    }

    var collapsed =
        controller.SetCollapsed(
            "kr.trading-clock",
            collapsed:
                true);

    if (collapsed.TotalDesiredHeightDip != 272)
    {
        throw new InvalidOperationException(
            "Collapse auto-height validation failed.");
    }

    var disabled =
        controller.SetEnabled(
            "kr.world-time-space",
            enabled:
                false);

    if (disabled.TotalDesiredHeightDip != 44)
    {
        throw new InvalidOperationException(
            "Disable auto-height validation failed.");
    }

    controller.SetEnabled(
        "kr.world-time-space",
        enabled:
            true);

    var grown =
        controller.UpdateMeasuredHeight(
            "kr.world-time-space",
            measuredDesiredHeightDip:
                268,
            maximumViewportHeightDip:
                300);

    if (
        grown.TotalDesiredHeightDip != 320
        || grown.EffectiveViewportHeightDip != 300
        || !grown.HostLevelScrollingRequired
    )
    {
        throw new InvalidOperationException(
            "Host-level boundary fallback validation failed.");
    }

    var reloaded =
        new WidgetHostLayoutController(
            new JsonWidgetHostStateStore(
                statePath));

    reloaded.RegisterOrUpdate(
        new WidgetHostRegistration(
            "kr.world-time-space",
            "KR World Time-Space",
            presentationWorld,
            10));

    var persisted =
        reloaded.RegisterOrUpdate(
            new WidgetHostRegistration(
                "kr.trading-clock",
                "KR Trading Clock",
                presentationTrading,
                20));

    if (
        !persisted.Widgets.Single(
            widget =>
                widget.WidgetId == "kr.trading-clock")
            .Collapsed
    )
    {
        throw new InvalidOperationException(
            "Collapsed-state persistence validation failed.");
    }

    var widgetId =
        "kr.fixture.framework";

    var declared =
        new HashSet<string>(
            new[]
            {
                WidgetCapabilityIds.HeightReport,
                WidgetCapabilityIds.DialogRequest,
                WidgetCapabilityIds.TrayIconRequest
            },
            StringComparer.OrdinalIgnoreCase);

    var approvals =
        new InMemoryWidgetCapabilityApprovalStore();

    approvals.SetApprovedCapabilities(
        widgetId,
        declared);

    var authorizer =
        new DefaultWidgetCapabilityAuthorizer(
            approvals,
            new InMemoryWidgetCapabilityAuditSink());

    var dialogCalled =
        false;

    var dialogBroker =
        new GovernedWidgetDialogBroker(
            authorizer,
            (_, request, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                dialogCalled =
                    request.DialogId == "add-city";

                return Task.FromResult(
                    new WidgetDialogResult(
                        Accepted:
                            true,
                        SelectedOptionId:
                            "lisbon"));
            });

    var dialog =
        await dialogBroker.RequestAsync(
            widgetId,
            declared,
            new WidgetDialogRequest(
                "add-city",
                "Add city",
                null,
                new[]
                {
                    new WidgetDialogOption(
                        "lisbon",
                        "Lisbon")
                }),
            CancellationToken.None);

    if (
        !dialogCalled
        || !dialog.Accepted
        || dialog.SelectedOptionId != "lisbon"
    )
    {
        throw new InvalidOperationException(
            "Floating-dialog broker validation failed.");
    }

    var applied =
        new List<WidgetTrayIconSelection>();

    var tray =
        new GovernedWidgetTrayIconBroker(
            authorizer,
            new[]
            {
                new WidgetTrayIconStateDefinition(
                    "corehost.default",
                    0,
                    "Default CoreHost tray state."),
                new WidgetTrayIconStateDefinition(
                    "fixture.green",
                    1000,
                    "Fixture green tray state.")
            },
            "corehost.default",
            (selection, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                applied.Add(
                    selection);

                return Task.CompletedTask;
            });

    var traySelected =
        await tray.SubmitAsync(
            widgetId,
            declared,
            new WidgetTrayIconRequest(
                "fixture.open",
                "fixture.green",
                500,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(5),
                "Fixture session is open."),
            CancellationToken.None);

    if (
        traySelected.IconStateKey != "fixture.green"
        || applied.Count != 1
    )
    {
        throw new InvalidOperationException(
            "Tray-icon approved-state arbitration validation failed.");
    }

    await tray.WithdrawAsync(
        widgetId,
        declared,
        "fixture.open",
        CancellationToken.None);

    if (tray.GetCurrent().IconStateKey != "corehost.default")
    {
        throw new InvalidOperationException(
            "Tray-icon fallback validation failed.");
    }

    await ExpectInvalidOperationAsync(
        () =>
            tray.SubmitAsync(
                widgetId,
                declared,
                new WidgetTrayIconRequest(
                    "fixture.unknown",
                    "fixture.unknown-state",
                    100,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddMinutes(
                        5),
                    "Fixture unknown tray state."),
                CancellationToken.None),
        "Unknown tray-icon state rejection validation failed.");

    await ExpectInvalidOperationAsync(
        () =>
            tray.SubmitAsync(
                widgetId,
                declared,
                new WidgetTrayIconRequest(
                    "fixture.expired",
                    "fixture.green",
                    100,
                    DateTimeOffset.UtcNow.AddMinutes(
                        -10),
                    DateTimeOffset.UtcNow.AddMinutes(
                        -5),
                    "Fixture expired tray request."),
                CancellationToken.None),
        "Tray-icon expiry fallback validation failed.");

    var declaredWithoutTrayIcon =
        new HashSet<string>(
            new[]
            {
                WidgetCapabilityIds.HeightReport,
                WidgetCapabilityIds.DialogRequest
            },
            StringComparer.OrdinalIgnoreCase);

    await ExpectInvalidOperationAsync(
        () =>
            tray.SubmitAsync(
                widgetId,
                declaredWithoutTrayIcon,
                new WidgetTrayIconRequest(
                    "fixture.denied",
                    "fixture.green",
                    100,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddMinutes(
                        5),
                    "Fixture denied tray request."),
                CancellationToken.None),
        "Tray-icon capability-denied validation failed.");

    var lowerPriority =
        await tray.SubmitAsync(
            widgetId,
            declared,
            new WidgetTrayIconRequest(
                "fixture.lower-priority",
                "fixture.green",
                100,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(
                    5),
                "Fixture lower-priority tray request."),
            CancellationToken.None);

    var higherPriority =
        await tray.SubmitAsync(
            widgetId,
            declared,
            new WidgetTrayIconRequest(
                "fixture.higher-priority",
                "fixture.green",
                900,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(
                    5),
                "Fixture higher-priority tray request."),
            CancellationToken.None);

    if (
        lowerPriority.RequestId != "fixture.lower-priority"
        || higherPriority.RequestId != "fixture.higher-priority"
        || tray.GetCurrent().RequestId != "fixture.higher-priority"
    )
    {
        throw new InvalidOperationException(
            "Tray-icon priority arbitration validation failed.");
    }

    await tray.WithdrawAsync(
        widgetId,
        declared,
        "fixture.lower-priority",
        CancellationToken.None);

    await tray.WithdrawAsync(
        widgetId,
        declared,
        "fixture.higher-priority",
        CancellationToken.None);

    if (tray.GetCurrent().IconStateKey != "corehost.default")
    {
        throw new InvalidOperationException(
            "Tray-icon post-negative-test fallback validation failed.");
    }

    Console.WriteLine(
        "Universal Widget framework smoke test passed.");
}
finally
{
    if (Directory.Exists(
        tempRoot))
    {
        Directory.Delete(
            tempRoot,
            recursive:
                true);
    }
}


static async Task ExpectInvalidOperationAsync(
    Func<Task> action,
    string message)
{
    try
    {
        await action();

        throw new InvalidOperationException(
            message);
    }
    catch (
        InvalidOperationException)
    {
    }
}
