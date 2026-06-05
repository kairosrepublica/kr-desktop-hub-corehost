using System.Collections.Concurrent;
using KRDesktopHub.Contracts;
using KRDesktopHub.Core;
using KRDesktopHub.Platform.Abstractions;

var eventBus =
    new InMemoryEventBus();

var power =
    new MutablePowerStateService(
        PowerState.AcPower);

var network =
    new MutableNetworkStateService(
        isAvailable:
            true);

using var session =
    new MutableSessionStateService(
        SessionState.Unlocked);

using var timeZone =
    new MutableTimeZoneChangeService(
        "UTC");

await using var resources =
    new PassiveResourceMonitorService();

using var coordinator =
    new SystemPolicyCoordinator(
        eventBus,
        power,
        network,
        session,
        timeZone,
        resources,
        new SystemPolicyEvaluator(
            CoreHostPolicyOptions.Recommended));

coordinator.SetPanelVisibility(
    isVisible:
        true);

var gate =
    new SystemPolicyWidgetExecutionGate(
        coordinator);

var widgetProfile =
    new WidgetWorkloadProfile(
        WidgetId:
            "kr.fixture.policy",
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

gate.RegisterOrUpdateProfile(
    widgetProfile);

AssertAllowed(
    gate.Evaluate(
        widgetProfile.WidgetId,
        WidgetPolicyWorkKind.General),
    "Baseline execution should be allowed.");

session.Set(
    SessionState.Locked);

AssertBlocked(
    gate.Evaluate(
        widgetProfile.WidgetId,
        WidgetPolicyWorkKind.NetworkRequest),
    WidgetExecutionPolicyBlockReason.NetworkRequestsSuppressed,
    "Locked-session network suppression failed.");

session.Set(
    SessionState.Unlocked);

coordinator.SetPanelVisibility(
    isVisible:
        false);

var hiddenVisualDecision =
    gate.Evaluate(
        widgetProfile.WidgetId,
        WidgetPolicyWorkKind.VisualRefresh);

AssertBlocked(
    hiddenVisualDecision,
    WidgetExecutionPolicyBlockReason.VisualRefreshSuppressed,
    "Hidden-panel visual-refresh suppression failed.");

if (hiddenVisualDecision.RefreshIntervalMultiplier !=
    2.0)
{
    throw new InvalidOperationException(
        "Hidden-panel refresh multiplier validation failed.");
}

coordinator.SetPanelVisibility(
    isVisible:
        true);

power.Set(
    PowerState.Battery);

var batteryDecision =
    gate.Evaluate(
        widgetProfile.WidgetId,
        WidgetPolicyWorkKind.General);

AssertBlocked(
    batteryDecision,
    WidgetExecutionPolicyBlockReason.ExecutionSuppressed,
    "Battery-mode low-priority execution suppression failed.");

if (batteryDecision.RefreshIntervalMultiplier !=
    4.0)
{
    throw new InvalidOperationException(
        "Battery-mode refresh multiplier validation failed.");
}

power.Set(
    PowerState.AcPower);

network.Set(
    isAvailable:
        false);

AssertBlocked(
    gate.Evaluate(
        widgetProfile.WidgetId,
        WidgetPolicyWorkKind.NetworkRequest),
    WidgetExecutionPolicyBlockReason.NetworkRequestsSuppressed,
    "Offline network-request suppression failed.");

network.Set(
    isAvailable:
        true);

await using var controller =
    new PolicyEnforcedWidgetRuntimeController(
        WidgetRuntimePolicy.Default,
        gate);

var countingWidget =
    new CountingWidget(
        widgetProfile.WidgetId);

controller.Register(
    countingWidget,
    widgetProfile);

var widgetContext =
    new DefaultWidgetContext(
        new ConsoleWidgetLogger(),
        new PeriodicWidgetScheduler(),
        new MemoryWidgetStateStore(),
        new MemoryWidgetSettingsStore(),
        eventBus,
        new CommandRegistry(),
        new SystemClock(),
        new PassThroughLocalizationService(),
        new NullWidgetNotificationClient());

await controller.InitializeAsync(
    widgetProfile.WidgetId,
    widgetContext,
    CancellationToken.None);

power.Set(
    PowerState.Battery);

try
{
    await controller.StartAsync(
        widgetProfile.WidgetId,
        CancellationToken.None);

    throw new InvalidOperationException(
        "Policy-suppressed lifecycle start unexpectedly executed.");
}
catch (WidgetPolicySuppressedException exception)
{
    if (
        exception.WidgetId !=
            widgetProfile.WidgetId
        || exception.Decision.BlockReason !=
            WidgetExecutionPolicyBlockReason.ExecutionSuppressed
    )
    {
        throw new InvalidOperationException(
            "Policy-suppressed lifecycle exception did not preserve decision metadata.");
    }
}

if (countingWidget.StartCount != 0)
{
    throw new InvalidOperationException(
        "Policy-suppressed lifecycle start reached the Widget.");
}

power.Set(
    PowerState.AcPower);

await controller.StartAsync(
    widgetProfile.WidgetId,
    CancellationToken.None);

await controller.PauseAsync(
    widgetProfile.WidgetId,
    CancellationToken.None);

await controller.ResumeAsync(
    widgetProfile.WidgetId,
    CancellationToken.None);

await controller.StopAsync(
    widgetProfile.WidgetId,
    CancellationToken.None);

if (
    countingWidget.StartCount != 1
    || countingWidget.ResumeCount != 1
    || countingWidget.StopCount != 1
)
{
    throw new InvalidOperationException(
        "Policy-enforced Widget lifecycle validation failed.");
}

var schedulerProfile =
    new WidgetWorkloadProfile(
        WidgetId:
            "kr.fixture.policy.scheduler",
        IsActive:
            true,
        IsLowPriority:
            false,
        IsNetworkHeavy:
            true,
        RequiresVisualRefresh:
            true,
        IsTimeSensitive:
            false);

gate.RegisterOrUpdateProfile(
    schedulerProfile);

await using var scheduler =
    new PolicyAwarePeriodicWidgetScheduler(
        gate,
        schedulerProfile.WidgetId,
        WidgetPolicyWorkKind.NetworkRequest);

var scheduledExecutions =
    0;

network.Set(
    isAvailable:
        false);

await scheduler.ScheduleAsync(
    "policy.scheduler.smoke",
    TimeSpan.FromMilliseconds(
        20),
    _ =>
    {
        Interlocked.Increment(
            ref scheduledExecutions);

        return Task.CompletedTask;
    },
    CancellationToken.None);

await Task.Delay(
    120);

var suppressedSnapshot =
    scheduler.GetSnapshot(
        "policy.scheduler.smoke");

if (
    scheduledExecutions != 0
    || suppressedSnapshot.SuppressedCycles < 1
    || suppressedSnapshot.LastDecision is null
    || suppressedSnapshot.LastDecision.BlockReason !=
        WidgetExecutionPolicyBlockReason.NetworkRequestsSuppressed
)
{
    throw new InvalidOperationException(
        "Policy-aware scheduler suppression validation failed.");
}

network.Set(
    isAvailable:
        true);

await Task.Delay(
    120);

var resumedSnapshot =
    scheduler.GetSnapshot(
        "policy.scheduler.smoke");

if (
    scheduledExecutions < 1
    || resumedSnapshot.ExecutedCycles < 1
)
{
    throw new InvalidOperationException(
        "Policy-aware scheduler recovery validation failed.");
}

await scheduler.CancelAsync(
    "policy.scheduler.smoke",
    CancellationToken.None);

Console.WriteLine(
    "Batch 8B2B Widget Runtime execution-policy smoke test passed.");

static void AssertAllowed(
    WidgetExecutionPolicyResult decision,
    string message)
{
    if (!decision.IsAllowed)
    {
        throw new InvalidOperationException(
            message);
    }
}

static void AssertBlocked(
    WidgetExecutionPolicyResult decision,
    WidgetExecutionPolicyBlockReason expectedReason,
    string message)
{
    if (
        decision.IsAllowed
        || decision.BlockReason !=
            expectedReason
    )
    {
        throw new InvalidOperationException(
            message);
    }
}

public sealed class CountingWidget
    : IKrWidget
{
    public CountingWidget(
        string widgetId)
    {
        Descriptor =
            new WidgetDescriptor(
                widgetId,
                "Counting Fixture Widget",
                new Version(
                    0,
                    1,
                    0),
                new Version(
                    1,
                    0,
                    0),
                new Version(
                    0,
                    1,
                    0),
                Array.Empty<string>());
    }

    public WidgetDescriptor Descriptor { get; }

    public int StartCount { get; private set; }

    public int ResumeCount { get; private set; }

    public int StopCount { get; private set; }

    public Task InitializeAsync(
        IWidgetContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(
            context);

        return Task.CompletedTask;
    }

    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        StartCount++;

        return Task.CompletedTask;
    }

    public Task PauseAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public Task ResumeAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ResumeCount++;

        return Task.CompletedTask;
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        StopCount++;

        return Task.CompletedTask;
    }
}

public sealed class MutablePowerStateService
    : IPowerStateService
{
    public MutablePowerStateService(
        PowerState state)
    {
        Current =
            state;
    }

    public event EventHandler<PowerStateChanged>? Changed;

    public PowerState Current { get; private set; }

    public void Set(
        PowerState state)
    {
        Current =
            state;

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
        SessionState state)
    {
        Current =
            state;
    }

    public event EventHandler<SessionStateChanged>? Changed;

    public SessionState Current { get; private set; }

    public void Set(
        SessionState state)
    {
        Current =
            state;

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

public sealed class MutableTimeZoneChangeService
    : ITimeZoneChangeService
{
    public MutableTimeZoneChangeService(
        string timeZoneId)
    {
        CurrentTimeZoneId =
            timeZoneId;
    }

    public event EventHandler<TimeZoneChanged>? Changed;

    public string CurrentTimeZoneId { get; private set; }

    public void Set(
        string timeZoneId)
    {
        var previousTimeZoneId =
            CurrentTimeZoneId;

        CurrentTimeZoneId =
            timeZoneId;

        Changed?.Invoke(
            this,
            new TimeZoneChanged(
                previousTimeZoneId,
                timeZoneId,
                DateTimeOffset.UtcNow));
    }

    public void Dispose()
    {
    }
}

public sealed class MemoryWidgetStateStore
    : IWidgetStateStore
{
    private readonly ConcurrentDictionary<
        string,
        object?> _values =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<T?> ReadAsync<T>(
        string key,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(
            key);

        if (
            _values.TryGetValue(
                key,
                out var value)
            && value is T typedValue
        )
        {
            return Task.FromResult<T?>(
                typedValue);
        }

        return Task.FromResult<T?>(
            default);
    }

    public Task WriteAsync<T>(
        string key,
        T value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(
            key);

        _values[key] =
            value;

        return Task.CompletedTask;
    }
}

public sealed class MemoryWidgetSettingsStore
    : IWidgetSettingsStore
{
    public Task<T> GetAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(
            key);

        return Task.FromResult(
            defaultValue);
    }
}

public sealed class PassThroughLocalizationService
    : ILocalizationService
{
    public string CurrentCultureName { get; private set; } =
        "en";

    public string Get(
        string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            key);

        return key;
    }

    public string Get(
        string key,
        params object[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            key);

        ArgumentNullException.ThrowIfNull(
            arguments);

        return key;
    }

    public Task SetCultureAsync(
        string cultureName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(
            cultureName);

        CurrentCultureName =
            cultureName;

        return Task.CompletedTask;
    }
}

public sealed class PassiveResourceMonitorService
    : IResourceMonitorService
{
    public event EventHandler<ResourceSample>? Sampled
    {
        add
        {
        }

        remove
        {
        }
    }

    public ResourceSample? Latest =>
        null;

    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}