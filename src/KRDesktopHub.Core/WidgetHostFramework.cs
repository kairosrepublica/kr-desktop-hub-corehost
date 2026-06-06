
using System.Collections.Concurrent;
using System.Text.Json;
using KRDesktopHub.Contracts;

namespace KRDesktopHub.Core;

public static class WidgetHostFrameworkDefaults
{
    public const double DefaultPopupWidthDip =
        600;

    public const double DefaultPopupHeightDip =
        720;

    public const double DefaultCollapsedHeightDip =
        44;

    public const double DefaultWidgetGapDip =
        8;
}

public sealed record WidgetHostViewportHeightDecision(
    double HostHeightDip,
    bool HostLevelScrollingRequired);

public static class WidgetHostViewportHeightPolicy
{
    public static WidgetHostViewportHeightDecision PreserveOrGrow(
        double currentHostHeightDip,
        double desiredContentHeightDip,
        double maximumWorkAreaHeightDip)
    {
        if (
            !double.IsFinite(
                desiredContentHeightDip)
            || desiredContentHeightDip <= 0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(desiredContentHeightDip));
        }

        if (
            !double.IsFinite(
                maximumWorkAreaHeightDip)
            || maximumWorkAreaHeightDip <= 0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumWorkAreaHeightDip));
        }

        var current =
            double.IsFinite(
                currentHostHeightDip)
            && currentHostHeightDip > 0
                ? currentHostHeightDip
                : WidgetHostFrameworkDefaults
                    .DefaultPopupHeightDip;

        var boundedCurrent =
            Math.Min(
                current,
                maximumWorkAreaHeightDip);

        var boundedDesired =
            Math.Min(
                desiredContentHeightDip,
                maximumWorkAreaHeightDip);

        var nextHeight =
            Math.Max(
                boundedCurrent,
                boundedDesired);

        return new WidgetHostViewportHeightDecision(
            nextHeight,
            HostLevelScrollingRequired:
                desiredContentHeightDip
                > nextHeight);
    }
}

public sealed record WidgetHostPersistentItem(
    string WidgetId,
    bool Enabled,
    bool Collapsed,
    int Order);

public sealed class WidgetHostPersistentDocument
{
    public int SchemaVersion { get; set; } =
        1;

    public List<WidgetHostPersistentItem> Widgets { get; set; } =
        new();
}

public sealed class JsonWidgetHostStateStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.SnakeCaseLower,

            PropertyNameCaseInsensitive =
                true,

            WriteIndented =
                true
        };

    private readonly object _saveGate =
        new();

    public JsonWidgetHostStateStore(
        string stateFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            stateFilePath);

        StateFilePath =
            Path.GetFullPath(
                stateFilePath);
    }

    public string StateFilePath { get; }

    public WidgetHostPersistentDocument LoadOrCreate()
    {
        try
        {
            if (!File.Exists(
                StateFilePath))
            {
                return new WidgetHostPersistentDocument();
            }

            var document =
                JsonSerializer.Deserialize<WidgetHostPersistentDocument>(
                    File.ReadAllText(
                        StateFilePath),
                    JsonOptions);

            return document is not null
                && document.SchemaVersion == 1
                    ? document
                    : new WidgetHostPersistentDocument();
        }
        catch (
            Exception exception)
            when (
                exception is IOException
                or UnauthorizedAccessException
                or JsonException)
        {
            return new WidgetHostPersistentDocument();
        }
    }

    public void Save(
        WidgetHostPersistentDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        var directory =
            Path.GetDirectoryName(
                StateFilePath);

        if (!string.IsNullOrWhiteSpace(
            directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        lock (_saveGate)
        {
            var temporaryPath =
                StateFilePath
                + "."
                + Guid.NewGuid().ToString("N")
                + ".tmp";

            try
            {
                File.WriteAllText(
                    temporaryPath,
                    JsonSerializer.Serialize(
                        document,
                        JsonOptions));

                File.Move(
                    temporaryPath,
                    StateFilePath,
                    overwrite:
                        true);
            }
            finally
            {
                if (File.Exists(
                    temporaryPath))
                {
                    File.Delete(
                        temporaryPath);
                }
            }
        }
    }
}

public sealed class WidgetHostLayoutController
{
    private readonly object _gate =
        new();

    private readonly JsonWidgetHostStateStore? _stateStore;

    private readonly Dictionary<string, WidgetHostRegistration> _registrations =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, WidgetHostPersistentItem> _persistent =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, double> _measuredHeights =
        new(StringComparer.OrdinalIgnoreCase);

    public WidgetHostLayoutController(
        JsonWidgetHostStateStore? stateStore = null)
    {
        _stateStore =
            stateStore;

        foreach (var item in
            stateStore?.LoadOrCreate().Widgets
            ?? Enumerable.Empty<WidgetHostPersistentItem>())
        {
            _persistent[item.WidgetId] =
                item;
        }
    }

    public event Action<WidgetHostLayoutSnapshot>? LayoutChanged;

    public WidgetHostLayoutSnapshot RegisterOrUpdate(
        WidgetHostRegistration registration,
        double maximumViewportHeightDip =
            double.PositiveInfinity)
    {
        ValidateRegistration(
            registration);

        WidgetHostLayoutSnapshot snapshot;

        lock (_gate)
        {
            _registrations[registration.WidgetId] =
                registration;

            if (!_persistent.ContainsKey(
                registration.WidgetId))
            {
                _persistent[registration.WidgetId] =
                    new WidgetHostPersistentItem(
                        registration.WidgetId,
                        registration.Presentation.DefaultEnabled,
                        registration.Presentation.DefaultCollapsed,
                        registration.Order);
            }

            if (!_measuredHeights.ContainsKey(
                registration.WidgetId))
            {
                _measuredHeights[registration.WidgetId] =
                    registration.Presentation.PreferredExpandedHeightDip;
            }

            SaveLocked();
            snapshot =
                BuildSnapshotLocked(
                    maximumViewportHeightDip);
        }

        LayoutChanged?.Invoke(
            snapshot);

        return snapshot;
    }

    public WidgetHostLayoutSnapshot SetEnabled(
        string widgetId,
        bool enabled,
        double maximumViewportHeightDip =
            double.PositiveInfinity)
    {
        return MutatePersistent(
            widgetId,
            item =>
                item with
                {
                    Enabled =
                        enabled
                },
            maximumViewportHeightDip);
    }

    public WidgetHostLayoutSnapshot SetCollapsed(
        string widgetId,
        bool collapsed,
        double maximumViewportHeightDip =
            double.PositiveInfinity)
    {
        return MutatePersistent(
            widgetId,
            item =>
                item with
                {
                    Collapsed =
                        collapsed
                },
            maximumViewportHeightDip);
    }

    public WidgetHostLayoutSnapshot SetOrder(
        string widgetId,
        int order,
        double maximumViewportHeightDip =
            double.PositiveInfinity)
    {
        return MutatePersistent(
            widgetId,
            item =>
                item with
                {
                    Order =
                        order
                },
            maximumViewportHeightDip);
    }

    public WidgetHostLayoutSnapshot UpdateMeasuredHeight(
        string widgetId,
        double measuredDesiredHeightDip,
        double maximumViewportHeightDip =
            double.PositiveInfinity)
    {
        if (!double.IsFinite(
            measuredDesiredHeightDip)
            || measuredDesiredHeightDip <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(measuredDesiredHeightDip));
        }

        WidgetHostLayoutSnapshot snapshot;

        lock (_gate)
        {
            _ =
                GetRegistrationLocked(
                    widgetId);

            _measuredHeights[widgetId] =
                measuredDesiredHeightDip;

            snapshot =
                BuildSnapshotLocked(
                    maximumViewportHeightDip);
        }

        LayoutChanged?.Invoke(
            snapshot);

        return snapshot;
    }

    public WidgetHostLayoutSnapshot GetLayout(
        double maximumViewportHeightDip =
            double.PositiveInfinity)
    {
        lock (_gate)
        {
            return BuildSnapshotLocked(
                maximumViewportHeightDip);
        }
    }

    private WidgetHostLayoutSnapshot MutatePersistent(
        string widgetId,
        Func<WidgetHostPersistentItem, WidgetHostPersistentItem> mutator,
        double maximumViewportHeightDip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        ArgumentNullException.ThrowIfNull(
            mutator);

        WidgetHostLayoutSnapshot snapshot;

        lock (_gate)
        {
            _ =
                GetRegistrationLocked(
                    widgetId);

            var existing =
                _persistent[widgetId];

            _persistent[widgetId] =
                mutator(
                    existing);

            SaveLocked();
            snapshot =
                BuildSnapshotLocked(
                    maximumViewportHeightDip);
        }

        LayoutChanged?.Invoke(
            snapshot);

        return snapshot;
    }

    private WidgetHostLayoutSnapshot BuildSnapshotLocked(
        double maximumViewportHeightDip)
    {
        if (
            !double.IsPositiveInfinity(
                maximumViewportHeightDip)
            && (
                !double.IsFinite(
                    maximumViewportHeightDip)
                || maximumViewportHeightDip <= 0
            )
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumViewportHeightDip));
        }

        var widgets =
            _registrations
                .Values
                .Select(
                    registration =>
                    {
                        var state =
                            _persistent[registration.WidgetId];

                        var measured =
                            _measuredHeights[registration.WidgetId];

                        var actual =
                            !state.Enabled
                                ? 0
                                : state.Collapsed
                                    ? registration
                                        .Presentation
                                        .MinimumCollapsedHeightDip
                                    : Math.Max(
                                        registration
                                            .Presentation
                                            .MinimumCollapsedHeightDip,
                                        measured);

                        return new WidgetHostSurfaceSnapshot(
                            registration.WidgetId,
                            registration.DisplayName,
                            state.Enabled,
                            state.Collapsed,
                            state.Order,
                            registration.Presentation.PreferredExpandedHeightDip,
                            registration.Presentation.MinimumCollapsedHeightDip,
                            measured,
                            actual);
                    })
                .OrderBy(
                    widget =>
                        widget.Order)
                .ThenBy(
                    widget =>
                        widget.WidgetId,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var visible =
            widgets
                .Where(
                    widget =>
                        widget.Enabled)
                .ToArray();

        var total =
            visible.Sum(
                widget =>
                    widget.ActualHeightDip)
            + Math.Max(
                0,
                visible.Length - 1)
                * WidgetHostFrameworkDefaults.DefaultWidgetGapDip;

        var scrollingRequired =
            !double.IsPositiveInfinity(
                maximumViewportHeightDip)
            && total > maximumViewportHeightDip;

        return new WidgetHostLayoutSnapshot(
            WidgetHostFrameworkDefaults.DefaultPopupWidthDip,
            total,
            scrollingRequired
                ? maximumViewportHeightDip
                : total,
            scrollingRequired,
            widgets);
    }

    private WidgetHostRegistration GetRegistrationLocked(
        string widgetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        return _registrations.TryGetValue(
            widgetId,
            out var registration)
                ? registration
                : throw new KeyNotFoundException(
                    $"Widget host registration was not found: {widgetId}");
    }

    private void SaveLocked()
    {
        _stateStore?.Save(
            new WidgetHostPersistentDocument
            {
                Widgets =
                    _persistent
                        .Values
                        .OrderBy(
                            item =>
                                item.Order)
                        .ThenBy(
                            item =>
                                item.WidgetId,
                            StringComparer.OrdinalIgnoreCase)
                        .ToList()
            });
    }

    private static void ValidateRegistration(
        WidgetHostRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(
            registration);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            registration.WidgetId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            registration.DisplayName);

        ArgumentNullException.ThrowIfNull(
            registration.Presentation);

        if (
            !double.IsFinite(
                registration.Presentation.PreferredExpandedHeightDip)
            || registration.Presentation.PreferredExpandedHeightDip <= 0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(registration));
        }

        if (
            !double.IsFinite(
                registration.Presentation.MinimumCollapsedHeightDip)
            || registration.Presentation.MinimumCollapsedHeightDip <= 0
            || registration.Presentation.MinimumCollapsedHeightDip
                > registration.Presentation.PreferredExpandedHeightDip
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(registration));
        }

        if (
            registration.Presentation.SettingsSchemaVersion < 1
            || registration.Presentation.StateSchemaVersion < 1
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(registration));
        }
    }
}



public sealed class IntegratedWidgetContext
    : IWidgetHostIntegrationContext
{
    private readonly IWidgetContext _inner;

    public IntegratedWidgetContext(
        IWidgetContext inner,
        IWidgetHostLayoutClient hostLayout,
        IWidgetDialogBroker dialogs,
        IWidgetTrayIconBroker trayIcons)
    {
        _inner =
            inner
            ?? throw new ArgumentNullException(
                nameof(inner));

        HostLayout =
            hostLayout
            ?? throw new ArgumentNullException(
                nameof(hostLayout));

        Dialogs =
            dialogs
            ?? throw new ArgumentNullException(
                nameof(dialogs));

        TrayIcons =
            trayIcons
            ?? throw new ArgumentNullException(
                nameof(trayIcons));
    }

    public IWidgetLogger Logger =>
        _inner.Logger;

    public IWidgetScheduler Scheduler =>
        _inner.Scheduler;

    public IWidgetStateStore StateStore =>
        _inner.StateStore;

    public IWidgetSettingsStore SettingsStore =>
        _inner.SettingsStore;

    public IEventBus EventBus =>
        _inner.EventBus;

    public ICommandRegistry Commands =>
        _inner.Commands;

    public IClock Clock =>
        _inner.Clock;

    public ILocalizationService Localization =>
        _inner.Localization;

    public IWidgetNotificationClient Notifications =>
        _inner.Notifications;

    public IWidgetHostLayoutClient HostLayout { get; }

    public IWidgetDialogBroker Dialogs { get; }

    public IWidgetTrayIconBroker TrayIcons { get; }
}

public sealed class GovernedWidgetHostLayoutClient
    : IWidgetHostLayoutClient
{
    private readonly IWidgetCapabilityAuthorizer _authorizer;
    private readonly WidgetHostLayoutController _controller;

    public GovernedWidgetHostLayoutClient(
        IWidgetCapabilityAuthorizer authorizer,
        WidgetHostLayoutController controller)
    {
        _authorizer =
            authorizer
            ?? throw new ArgumentNullException(
                nameof(authorizer));

        _controller =
            controller
            ?? throw new ArgumentNullException(
                nameof(controller));
    }

    public Task<WidgetHostLayoutSnapshot> ReportDesiredHeightAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        double desiredHeightDip,
        double maximumViewportHeightDip,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WidgetCapabilityGuard.EnsureAllowed(
            _authorizer.Authorize(
                widgetId,
                declaredCapabilities,
                WidgetCapabilityIds.HeightReport));

        return Task.FromResult(
            _controller.UpdateMeasuredHeight(
                widgetId,
                desiredHeightDip,
                maximumViewportHeightDip));
    }
}

public delegate Task<WidgetDialogResult> WidgetDialogPresenter(
    string widgetId,
    WidgetDialogRequest request,
    CancellationToken cancellationToken);

public sealed class GovernedWidgetDialogBroker
    : IWidgetDialogBroker
{
    private readonly IWidgetCapabilityAuthorizer _authorizer;
    private readonly WidgetDialogPresenter _presenter;

    public GovernedWidgetDialogBroker(
        IWidgetCapabilityAuthorizer authorizer,
        WidgetDialogPresenter presenter)
    {
        _authorizer =
            authorizer
            ?? throw new ArgumentNullException(
                nameof(authorizer));

        _presenter =
            presenter
            ?? throw new ArgumentNullException(
                nameof(presenter));
    }

    public Task<WidgetDialogResult> RequestAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        WidgetDialogRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(
            request);

        if (
            string.IsNullOrWhiteSpace(
                request.DialogId)
            || string.IsNullOrWhiteSpace(
                request.Title)
        )
        {
            throw new InvalidOperationException(
                "Widget dialog ID and title are required.");
        }

        WidgetCapabilityGuard.EnsureAllowed(
            _authorizer.Authorize(
                widgetId,
                declaredCapabilities,
                WidgetCapabilityIds.DialogRequest));

        return _presenter(
            widgetId,
            request,
            cancellationToken);
    }
}

public sealed record WidgetTrayIconStateDefinition(
    string IconStateKey,
    int MaximumPriority,
    string Description);

public sealed class GovernedWidgetTrayIconBroker
    : IWidgetTrayIconBroker
{
    private sealed record ActiveRequest(
        string WidgetId,
        WidgetTrayIconRequest Request);

    private readonly object _gate =
        new();

    private readonly IWidgetCapabilityAuthorizer _authorizer;
    private readonly IReadOnlyDictionary<string, WidgetTrayIconStateDefinition> _definitions;
    private readonly Func<WidgetTrayIconSelection, CancellationToken, Task> _applySelection;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Dictionary<string, ActiveRequest> _active =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly string _fallbackStateKey;
    private WidgetTrayIconSelection _current;

    public GovernedWidgetTrayIconBroker(
        IWidgetCapabilityAuthorizer authorizer,
        IEnumerable<WidgetTrayIconStateDefinition> definitions,
        string fallbackStateKey,
        Func<WidgetTrayIconSelection, CancellationToken, Task> applySelection,
        Func<DateTimeOffset>? utcNow = null)
    {
        _authorizer =
            authorizer
            ?? throw new ArgumentNullException(
                nameof(authorizer));

        ArgumentNullException.ThrowIfNull(
            definitions);

        _definitions =
            definitions.ToDictionary(
                definition =>
                    definition.IconStateKey,
                StringComparer.OrdinalIgnoreCase);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            fallbackStateKey);

        if (!_definitions.ContainsKey(
            fallbackStateKey))
        {
            throw new InvalidOperationException(
                "Fallback tray-icon state must be present in the approved state registry.");
        }

        _fallbackStateKey =
            fallbackStateKey;

        _applySelection =
            applySelection
            ?? throw new ArgumentNullException(
                nameof(applySelection));

        _utcNow =
            utcNow
            ?? (() => DateTimeOffset.UtcNow);

        _current =
            CreateFallbackSelection(
                _utcNow());
    }

    public async Task<WidgetTrayIconSelection> SubmitAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        WidgetTrayIconRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(
            request);

        WidgetCapabilityGuard.EnsureAllowed(
            _authorizer.Authorize(
                widgetId,
                declaredCapabilities,
                WidgetCapabilityIds.TrayIconRequest));

        if (string.IsNullOrWhiteSpace(
            request.RequestId))
        {
            throw new InvalidOperationException(
                "Tray-icon request ID is required.");
        }

        if (!_definitions.TryGetValue(
            request.IconStateKey,
            out var definition))
        {
            throw new InvalidOperationException(
                $"Tray-icon state is not approved: {request.IconStateKey}");
        }

        if (
            request.Priority < 0
            || request.Priority > definition.MaximumPriority
        )
        {
            throw new InvalidOperationException(
                $"Tray-icon request priority is outside the approved range for {request.IconStateKey}.");
        }

        var now =
            _utcNow();

        if (
            request.ExpiresAtUtc is not null
            && request.ExpiresAtUtc <= now
        )
        {
            throw new InvalidOperationException(
                "Tray-icon request is already expired.");
        }

        WidgetTrayIconSelection selected;

        lock (_gate)
        {
            _active[CreateActiveKey(
                widgetId,
                request.RequestId)] =
                    new ActiveRequest(
                        widgetId,
                        request);

            selected =
                SelectLocked(
                    now);

            _current =
                selected;
        }

        await _applySelection(
            selected,
            cancellationToken);

        return selected;
    }

    public async Task<WidgetTrayIconSelection> WithdrawAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        string requestId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(
            requestId);

        WidgetCapabilityGuard.EnsureAllowed(
            _authorizer.Authorize(
                widgetId,
                declaredCapabilities,
                WidgetCapabilityIds.TrayIconRequest));

        WidgetTrayIconSelection selected;

        lock (_gate)
        {
            _active.Remove(
                CreateActiveKey(
                    widgetId,
                    requestId));

            selected =
                SelectLocked(
                    _utcNow());

            _current =
                selected;
        }

        await _applySelection(
            selected,
            cancellationToken);

        return selected;
    }

    public WidgetTrayIconSelection GetCurrent()
    {
        lock (_gate)
        {
            _current =
                SelectLocked(
                    _utcNow());

            return _current;
        }
    }

    private WidgetTrayIconSelection SelectLocked(
        DateTimeOffset now)
    {
        foreach (var key in
            _active
                .Where(
                    pair =>
                        pair.Value.Request.ExpiresAtUtc is not null
                        && pair.Value.Request.ExpiresAtUtc <= now)
                .Select(
                    pair =>
                        pair.Key)
                .ToArray())
        {
            _active.Remove(
                key);
        }

        var winner =
            _active
                .Values
                .OrderByDescending(
                    value =>
                        value.Request.Priority)
                .ThenByDescending(
                    value =>
                        value.Request.IssuedAtUtc)
                .ThenBy(
                    value =>
                        value.WidgetId,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    value =>
                        value.Request.RequestId,
                    StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

        return winner is null
            ? CreateFallbackSelection(
                now)
            : new WidgetTrayIconSelection(
                winner.Request.RequestId,
                winner.WidgetId,
                winner.Request.IconStateKey,
                winner.Request.Priority,
                now,
                winner.Request.Reason);
    }

    private WidgetTrayIconSelection CreateFallbackSelection(
        DateTimeOffset now)
    {
        return new WidgetTrayIconSelection(
            RequestId:
                null,
            WidgetId:
                null,
            IconStateKey:
                _fallbackStateKey,
            Priority:
                0,
            AppliedAtUtc:
                now,
            Reason:
                "No active approved Widget tray-icon request.");
    }

    private static string CreateActiveKey(
        string widgetId,
        string requestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        return widgetId
            + "\n"
            + requestId;
    }
}

internal static class WidgetCapabilityGuard
{
    public static void EnsureAllowed(
        WidgetCapabilityDecision decision)
    {
        if (!decision.IsAllowed)
        {
            throw new WidgetCapabilityDeniedException(
                decision);
        }
    }
}
