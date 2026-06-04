using KRDesktopHub.Contracts;
using KRDesktopHub.Core;
using KRDesktopHub.Platform.Abstractions;
using KRDesktopHub.Platform.Windows;

var options =
    CoreHostPolicyOptions.Recommended;

options.Validate();

var evaluator =
    new SystemPolicyEvaluator(
        options);

var profile =
    new WidgetWorkloadProfile(
        WidgetId:
            "kr.fixture.network-heavy",

        IsActive:
            true,

        IsLowPriority:
            true,

        IsNetworkHeavy:
            true,

        RequiresVisualRefresh:
            true,

        IsTimeSensitive:
            true);

var constrainedState =
    new SystemPolicyState(
        PowerState:
            PowerState.Battery,

        IsNetworkAvailable:
            true,

        SessionState:
            SessionState.Locked,

        TimeZoneId:
            TimeZoneInfo.Local.Id,

        IsPanelVisible:
            false,

        LastResumeAtUtc:
            DateTimeOffset.UtcNow);

var constrainedDecision =
    evaluator.Evaluate(
        profile,
        constrainedState);

if (constrainedDecision.AllowExecution)
{
    throw new InvalidOperationException(
        "Battery policy validation failed.");
}

if (constrainedDecision.AllowNetworkRequests)
{
    throw new InvalidOperationException(
        "Locked-session network policy validation failed.");
}

if (constrainedDecision.AllowVisualRefresh)
{
    throw new InvalidOperationException(
        "Hidden-panel visual-refresh policy validation failed.");
}

if (constrainedDecision.RefreshIntervalMultiplier != 4.0)
{
    throw new InvalidOperationException(
        "Battery refresh-multiplier validation failed.");
}

if (!constrainedDecision.RefreshAfterTimeZoneChange)
{
    throw new InvalidOperationException(
        "Time-sensitive Widget policy validation failed.");
}

var resumedAtUtc =
    DateTimeOffset.UtcNow;

if (!evaluator.ShouldRefreshAfterResume(
    resumedAtUtc - TimeSpan.FromMinutes(10),
    resumedAtUtc,
    staleAfter:
        TimeSpan.FromMinutes(5)))
{
    throw new InvalidOperationException(
        "Resume stale-data refresh validation failed.");
}

if (evaluator.ShouldRefreshAfterResume(
    resumedAtUtc - TimeSpan.FromMinutes(1),
    resumedAtUtc,
    staleAfter:
        TimeSpan.FromMinutes(5)))
{
    throw new InvalidOperationException(
        "Resume fresh-data suppression validation failed.");
}

var eventBus =
    new InMemoryEventBus();

var observedSignals =
    new List<SystemPolicySignal>();

using var subscription =
    eventBus.Subscribe<SystemPolicySignal>(
        (signal, _) =>
        {
            lock (observedSignals)
            {
                observedSignals.Add(
                    signal);
            }

            return Task.CompletedTask;
        });

var power =
    new MutablePowerStateService(
        PowerState.AcPower);

var network =
    new MutableNetworkStateService(
        isAvailable:
            true);

var session =
    new MutableSessionStateService(
        SessionState.Unlocked);

var timeZone =
    new MutableTimeZoneService(
        "Initial/Zone");

await using var resources =
    new WindowsProcessResourceMonitorService(
        TimeSpan.FromMilliseconds(30));

using var coordinator =
    new SystemPolicyCoordinator(
        eventBus,
        power,
        network,
        session,
        timeZone,
        resources);

coordinator.SetPanelVisibility(
    isVisible:
        true);

network.Set(
    isAvailable:
        false);

session.Set(
    SessionState.Locked);

power.Set(
    PowerState.Resumed);

timeZone.Set(
    "Changed/Zone");

await resources.StartAsync(
    CancellationToken.None);

await Task.Delay(
    150);

await resources.StopAsync(
    CancellationToken.None);

if (resources.Latest is null)
{
    throw new InvalidOperationException(
        "Resource monitor sampling failed.");
}

if (resources.Latest.WorkingSetBytes <= 0)
{
    throw new InvalidOperationException(
        "Working-set sampling failed.");
}

await Task.Delay(
    50);

var current =
    coordinator.Current;

if (current.IsNetworkAvailable)
{
    throw new InvalidOperationException(
        "Coordinator network-state validation failed.");
}

if (current.SessionState !=
    SessionState.Locked)
{
    throw new InvalidOperationException(
        "Coordinator session-state validation failed.");
}

if (current.TimeZoneId !=
    "Changed/Zone")
{
    throw new InvalidOperationException(
        "Coordinator time-zone-state validation failed.");
}

if (current.LastResumeAtUtc is null)
{
    throw new InvalidOperationException(
        "Coordinator resume-state validation failed.");
}

lock (observedSignals)
{
    if (!observedSignals.Any(
        signal =>
            signal.Type ==
                SystemPolicySignalType.ResourceSampled))
    {
        throw new InvalidOperationException(
            "Coordinator resource-event validation failed.");
    }

    if (!observedSignals.Any(
        signal =>
            signal.Type ==
                SystemPolicySignalType.TimeZoneChanged))
    {
        throw new InvalidOperationException(
            "Coordinator time-zone-event validation failed.");
    }
}

using (var windowsPower =
    new WindowsPowerStateService())
{
    _ =
        windowsPower.Current;
}

using (var windowsNetwork =
    new WindowsNetworkStateService())
{
    _ =
        windowsNetwork.IsAvailable;
}

using (var windowsSession =
    new WindowsSessionStateService())
{
    _ =
        windowsSession.Current;
}

using (var windowsTimeZone =
    new WindowsTimeZoneChangeService())
{
    if (string.IsNullOrWhiteSpace(
        windowsTimeZone.CurrentTimeZoneId))
    {
        throw new InvalidOperationException(
            "Windows time-zone adapter validation failed.");
    }
}

Console.WriteLine(
    "Batch 5 System Policies smoke test passed.");

public sealed class MutablePowerStateService
    : IPowerStateService
{
    public MutablePowerStateService(
        PowerState current)
    {
        Current = current;
    }

    public event EventHandler<PowerStateChanged>? Changed;

    public PowerState Current { get; private set; }

    public void Set(
        PowerState state)
    {
        Current = state;

        Changed?.Invoke(
            this,
            new PowerStateChanged(
                state,
                DateTimeOffset.UtcNow));
    }
}

public sealed class MutableNetworkStateService
    : INetworkStateService
{
    public MutableNetworkStateService(
        bool isAvailable)
    {
        IsAvailable =
            isAvailable;
    }

    public event EventHandler<NetworkStateChanged>? Changed;

    public bool IsAvailable { get; private set; }

    public void Set(
        bool isAvailable)
    {
        IsAvailable =
            isAvailable;

        Changed?.Invoke(
            this,
            new NetworkStateChanged(
                isAvailable,
                DateTimeOffset.UtcNow));
    }
}

public sealed class MutableSessionStateService
    : ISessionStateService
{
    public MutableSessionStateService(
        SessionState current)
    {
        Current = current;
    }

    public event EventHandler<SessionStateChanged>? Changed;

    public SessionState Current { get; private set; }

    public void Set(
        SessionState state)
    {
        Current = state;

        Changed?.Invoke(
            this,
            new SessionStateChanged(
                state,
                DateTimeOffset.UtcNow));
    }

    public void Dispose()
    {
    }
}

public sealed class MutableTimeZoneService
    : ITimeZoneChangeService
{
    public MutableTimeZoneService(
        string currentTimeZoneId)
    {
        CurrentTimeZoneId =
            currentTimeZoneId;
    }

    public event EventHandler<TimeZoneChanged>? Changed;

    public string CurrentTimeZoneId { get; private set; }

    public void Set(
        string currentTimeZoneId)
    {
        var previous =
            CurrentTimeZoneId;

        CurrentTimeZoneId =
            currentTimeZoneId;

        Changed?.Invoke(
            this,
            new TimeZoneChanged(
                previous,
                currentTimeZoneId,
                DateTimeOffset.UtcNow));
    }

    public void Dispose()
    {
    }
}