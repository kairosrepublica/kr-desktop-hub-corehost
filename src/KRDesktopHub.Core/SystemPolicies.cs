using KRDesktopHub.Contracts;
using KRDesktopHub.Platform.Abstractions;

namespace KRDesktopHub.Core;

public sealed record CoreHostPolicyOptions(
    bool RefreshOnlyStaleWidgetsAfterResume,
    bool ReplayMissedScheduledRunsAfterResume,
    bool PauseNetworkHeavyWidgetsWhenLocked,
    bool PauseLowPriorityWidgetsOnBattery,
    bool RefreshTimeWidgetsAfterTimeZoneChange,
    bool RetryFailedTasksAfterNetworkRecovery,
    bool StopVisualRefreshWhenPanelHidden,
    bool StopNetworkRequestsWhenWidgetInactive,
    TimeSpan NetworkRecoveryDebounce,
    TimeSpan ResourceSampleInterval,
    double? IdleCpuWarningPercent,
    long? IdleWorkingSetWarningBytes)
{
    public static CoreHostPolicyOptions Recommended =>
        new(
            RefreshOnlyStaleWidgetsAfterResume:
                true,

            ReplayMissedScheduledRunsAfterResume:
                false,

            PauseNetworkHeavyWidgetsWhenLocked:
                true,

            PauseLowPriorityWidgetsOnBattery:
                true,

            RefreshTimeWidgetsAfterTimeZoneChange:
                true,

            RetryFailedTasksAfterNetworkRecovery:
                true,

            StopVisualRefreshWhenPanelHidden:
                true,

            StopNetworkRequestsWhenWidgetInactive:
                true,

            NetworkRecoveryDebounce:
                TimeSpan.FromSeconds(5),

            ResourceSampleInterval:
                TimeSpan.FromSeconds(30),

            IdleCpuWarningPercent:
                null,

            IdleWorkingSetWarningBytes:
                null);

    public void Validate()
    {
        if (NetworkRecoveryDebounce < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(NetworkRecoveryDebounce));
        }

        if (ResourceSampleInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ResourceSampleInterval));
        }

        if (IdleCpuWarningPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(IdleCpuWarningPercent));
        }

        if (IdleWorkingSetWarningBytes is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(IdleWorkingSetWarningBytes));
        }
    }
}

public sealed record WidgetWorkloadProfile(
    string WidgetId,
    bool IsActive,
    bool IsLowPriority,
    bool IsNetworkHeavy,
    bool RequiresVisualRefresh,
    bool IsTimeSensitive);

public sealed record SystemPolicyState(
    PowerState PowerState,
    bool IsNetworkAvailable,
    SessionState SessionState,
    string TimeZoneId,
    bool IsPanelVisible,
    DateTimeOffset? LastResumeAtUtc);

public sealed record WidgetPolicyDecision(
    bool AllowExecution,
    bool AllowNetworkRequests,
    bool AllowVisualRefresh,
    double RefreshIntervalMultiplier,
    bool RefreshIfStaleAfterResume,
    bool RefreshAfterTimeZoneChange,
    TimeSpan NetworkRecoveryDelay);

public sealed class SystemPolicyEvaluator
{
    private readonly CoreHostPolicyOptions _options;

    public SystemPolicyEvaluator(
        CoreHostPolicyOptions options)
    {
        options.Validate();
        _options = options;
    }

    public WidgetPolicyDecision Evaluate(
        WidgetWorkloadProfile widget,
        SystemPolicyState state)
    {
        ArgumentNullException.ThrowIfNull(widget);
        ArgumentNullException.ThrowIfNull(state);

        var isBatteryMode =
            state.PowerState is
                PowerState.Battery
                or PowerState.BatterySaver;

        var allowExecution =
            !(isBatteryMode
                && _options.PauseLowPriorityWidgetsOnBattery
                && widget.IsLowPriority);

        var allowNetworkRequests =
            state.IsNetworkAvailable;

        if (_options.StopNetworkRequestsWhenWidgetInactive
            && !widget.IsActive)
        {
            allowNetworkRequests =
                false;
        }

        if (_options.PauseNetworkHeavyWidgetsWhenLocked
            && state.SessionState == SessionState.Locked
            && widget.IsNetworkHeavy)
        {
            allowNetworkRequests =
                false;
        }

        if (!allowExecution)
        {
            allowNetworkRequests =
                false;
        }

        var allowVisualRefresh =
            widget.RequiresVisualRefresh
            && (
                state.IsPanelVisible
                || !_options.StopVisualRefreshWhenPanelHidden);

        var refreshIntervalMultiplier =
            isBatteryMode
            && widget.IsLowPriority
                ? 4.0
                : state.IsPanelVisible
                    ? 1.0
                    : 2.0;

        return new WidgetPolicyDecision(
            AllowExecution:
                allowExecution,

            AllowNetworkRequests:
                allowNetworkRequests,

            AllowVisualRefresh:
                allowVisualRefresh,

            RefreshIntervalMultiplier:
                refreshIntervalMultiplier,

            RefreshIfStaleAfterResume:
                _options.RefreshOnlyStaleWidgetsAfterResume,

            RefreshAfterTimeZoneChange:
                _options.RefreshTimeWidgetsAfterTimeZoneChange
                && widget.IsTimeSensitive,

            NetworkRecoveryDelay:
                _options.RetryFailedTasksAfterNetworkRecovery
                    ? _options.NetworkRecoveryDebounce
                    : Timeout.InfiniteTimeSpan);
    }

    public bool ShouldRefreshAfterResume(
        DateTimeOffset? lastSuccessAtUtc,
        DateTimeOffset resumedAtUtc,
        TimeSpan staleAfter)
    {
        if (!_options.RefreshOnlyStaleWidgetsAfterResume)
        {
            return true;
        }

        if (lastSuccessAtUtc is null)
        {
            return true;
        }

        return resumedAtUtc - lastSuccessAtUtc.Value
            >= staleAfter;
    }
}

public enum SystemPolicySignalType
{
    PowerChanged,
    NetworkChanged,
    SessionChanged,
    TimeZoneChanged,
    ResourceSampled
}

public sealed record SystemPolicySignal(
    SystemPolicySignalType Type,
    DateTimeOffset OccurredAtUtc,
    string Summary);

public sealed class SystemPolicyCoordinator
    : IDisposable
{
    private readonly object _sync = new();
    private readonly IEventBus _eventBus;
    private readonly IPowerStateService _power;
    private readonly INetworkStateService _network;
    private readonly ISessionStateService _session;
    private readonly ITimeZoneChangeService _timeZone;
    private readonly IResourceMonitorService _resources;

    private SystemPolicyState _state;

    public SystemPolicyCoordinator(
        IEventBus eventBus,
        IPowerStateService power,
        INetworkStateService network,
        ISessionStateService session,
        ITimeZoneChangeService timeZone,
        IResourceMonitorService resources)
    {
        _eventBus = eventBus;
        _power = power;
        _network = network;
        _session = session;
        _timeZone = timeZone;
        _resources = resources;

        _state =
            new SystemPolicyState(
                power.Current,
                network.IsAvailable,
                session.Current,
                timeZone.CurrentTimeZoneId,
                IsPanelVisible:
                    false,

                LastResumeAtUtc:
                    null);

        _power.Changed += OnPowerChanged;
        _network.Changed += OnNetworkChanged;
        _session.Changed += OnSessionChanged;
        _timeZone.Changed += OnTimeZoneChanged;
        _resources.Sampled += OnResourceSampled;
    }

    public SystemPolicyState Current
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    public void SetPanelVisibility(
        bool isVisible)
    {
        lock (_sync)
        {
            _state =
                _state with
                {
                    IsPanelVisible = isVisible
                };
        }
    }

    public void Dispose()
    {
        _power.Changed -= OnPowerChanged;
        _network.Changed -= OnNetworkChanged;
        _session.Changed -= OnSessionChanged;
        _timeZone.Changed -= OnTimeZoneChanged;
        _resources.Sampled -= OnResourceSampled;
    }

    private void OnPowerChanged(
        object? sender,
        PowerStateChanged change)
    {
        lock (_sync)
        {
            _state =
                _state with
                {
                    PowerState = change.State,
                    LastResumeAtUtc =
                        change.State == PowerState.Resumed
                            ? change.ChangedAtUtc
                            : _state.LastResumeAtUtc
                };
        }

        Publish(
            new SystemPolicySignal(
                SystemPolicySignalType.PowerChanged,
                change.ChangedAtUtc,
                change.State.ToString()));
    }

    private void OnNetworkChanged(
        object? sender,
        NetworkStateChanged change)
    {
        lock (_sync)
        {
            _state =
                _state with
                {
                    IsNetworkAvailable =
                        change.IsAvailable
                };
        }

        Publish(
            new SystemPolicySignal(
                SystemPolicySignalType.NetworkChanged,
                change.ChangedAtUtc,
                change.IsAvailable
                    ? "Available"
                    : "Unavailable"));
    }

    private void OnSessionChanged(
        object? sender,
        SessionStateChanged change)
    {
        lock (_sync)
        {
            _state =
                _state with
                {
                    SessionState =
                        change.State
                };
        }

        Publish(
            new SystemPolicySignal(
                SystemPolicySignalType.SessionChanged,
                change.ChangedAtUtc,
                change.State.ToString()));
    }

    private void OnTimeZoneChanged(
        object? sender,
        TimeZoneChanged change)
    {
        lock (_sync)
        {
            _state =
                _state with
                {
                    TimeZoneId =
                        change.CurrentTimeZoneId
                };
        }

        Publish(
            new SystemPolicySignal(
                SystemPolicySignalType.TimeZoneChanged,
                change.ChangedAtUtc,
                $"{change.PreviousTimeZoneId} -> {change.CurrentTimeZoneId}"));
    }

    private void OnResourceSampled(
        object? sender,
        ResourceSample sample)
    {
        Publish(
            new SystemPolicySignal(
                SystemPolicySignalType.ResourceSampled,
                sample.SampledAtUtc,
                $"CPU={sample.CpuPercent:F2}%; WorkingSet={sample.WorkingSetBytes}"));
    }

    private void Publish(
        SystemPolicySignal signal)
    {
        _ = _eventBus.PublishAsync(
            signal,
            CancellationToken.None);
    }
}