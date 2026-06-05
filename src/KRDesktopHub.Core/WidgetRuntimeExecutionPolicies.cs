using System.Collections.Concurrent;
using KRDesktopHub.Contracts;

namespace KRDesktopHub.Core;

public enum WidgetPolicyWorkKind
{
    General,
    NetworkRequest,
    VisualRefresh
}

public enum WidgetExecutionPolicyBlockReason
{
    None,
    ExecutionSuppressed,
    NetworkRequestsSuppressed,
    VisualRefreshSuppressed
}

public sealed record WidgetExecutionPolicyResult(
    bool IsAllowed,
    WidgetExecutionPolicyBlockReason BlockReason,
    double RefreshIntervalMultiplier,
    WidgetPolicyDecision SystemDecision);

public interface IWidgetExecutionPolicyGate
{
    void RegisterOrUpdateProfile(
        WidgetWorkloadProfile profile);

    WidgetExecutionPolicyResult Evaluate(
        string widgetId,
        WidgetPolicyWorkKind workKind);
}

public sealed class SystemPolicyWidgetExecutionGate
    : IWidgetExecutionPolicyGate
{
    private readonly SystemPolicyCoordinator _coordinator;

    private readonly ConcurrentDictionary<
        string,
        WidgetWorkloadProfile> _profiles =
        new(StringComparer.OrdinalIgnoreCase);

    public SystemPolicyWidgetExecutionGate(
        SystemPolicyCoordinator coordinator)
    {
        ArgumentNullException.ThrowIfNull(
            coordinator);

        _coordinator =
            coordinator;
    }

    public void RegisterOrUpdateProfile(
        WidgetWorkloadProfile profile)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            profile.WidgetId);

        _profiles.AddOrUpdate(
            profile.WidgetId,
            profile,
            (_, _) =>
                profile);
    }

    public WidgetExecutionPolicyResult Evaluate(
        string widgetId,
        WidgetPolicyWorkKind workKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        if (!_profiles.TryGetValue(
            widgetId,
            out var profile))
        {
            throw new KeyNotFoundException(
                $"Widget workload profile is not registered: {widgetId}");
        }

        var decision =
            _coordinator.Evaluate(
                profile);

        if (!decision.AllowExecution)
        {
            return new WidgetExecutionPolicyResult(
                IsAllowed:
                    false,
                BlockReason:
                    WidgetExecutionPolicyBlockReason.ExecutionSuppressed,
                RefreshIntervalMultiplier:
                    decision.RefreshIntervalMultiplier,
                SystemDecision:
                    decision);
        }

        if (
            workKind ==
                WidgetPolicyWorkKind.NetworkRequest
            && !decision.AllowNetworkRequests
        )
        {
            return new WidgetExecutionPolicyResult(
                IsAllowed:
                    false,
                BlockReason:
                    WidgetExecutionPolicyBlockReason.NetworkRequestsSuppressed,
                RefreshIntervalMultiplier:
                    decision.RefreshIntervalMultiplier,
                SystemDecision:
                    decision);
        }

        if (
            workKind ==
                WidgetPolicyWorkKind.VisualRefresh
            && !decision.AllowVisualRefresh
        )
        {
            return new WidgetExecutionPolicyResult(
                IsAllowed:
                    false,
                BlockReason:
                    WidgetExecutionPolicyBlockReason.VisualRefreshSuppressed,
                RefreshIntervalMultiplier:
                    decision.RefreshIntervalMultiplier,
                SystemDecision:
                    decision);
        }

        return new WidgetExecutionPolicyResult(
            IsAllowed:
                true,
            BlockReason:
                WidgetExecutionPolicyBlockReason.None,
            RefreshIntervalMultiplier:
                decision.RefreshIntervalMultiplier,
            SystemDecision:
                decision);
    }
}

public sealed class WidgetPolicySuppressedException
    : InvalidOperationException
{
    public WidgetPolicySuppressedException(
        string widgetId,
        WidgetPolicyWorkKind workKind,
        WidgetExecutionPolicyResult decision)
        : base(
            CreateMessage(
                widgetId,
                workKind,
                decision))
    {
        WidgetId =
            widgetId;

        WorkKind =
            workKind;

        Decision =
            decision;
    }

    public string WidgetId { get; }

    public WidgetPolicyWorkKind WorkKind { get; }

    public WidgetExecutionPolicyResult Decision { get; }

    private static string CreateMessage(
        string widgetId,
        WidgetPolicyWorkKind workKind,
        WidgetExecutionPolicyResult decision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        ArgumentNullException.ThrowIfNull(
            decision);

        return $"Widget operation suppressed by system policy: {widgetId}; work={workKind}; reason={decision.BlockReason}.";
    }
}

public sealed class PolicyEnforcedWidgetRuntimeController
    : IAsyncDisposable
{
    private readonly WidgetRuntimeController _inner;

    private readonly IWidgetExecutionPolicyGate _policyGate;

    public PolicyEnforcedWidgetRuntimeController(
        WidgetRuntimePolicy runtimePolicy,
        IWidgetExecutionPolicyGate policyGate)
    {
        ArgumentNullException.ThrowIfNull(
            runtimePolicy);

        ArgumentNullException.ThrowIfNull(
            policyGate);

        _inner =
            new WidgetRuntimeController(
                runtimePolicy);

        _policyGate =
            policyGate;
    }

    public void Register(
        IKrWidget widget,
        WidgetWorkloadProfile profile)
    {
        ArgumentNullException.ThrowIfNull(
            widget);

        ArgumentNullException.ThrowIfNull(
            profile);

        if (!string.Equals(
            widget.Descriptor.WidgetId,
            profile.WidgetId,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Widget descriptor ID does not match workload-profile ID.");
        }

        _policyGate.RegisterOrUpdateProfile(
            profile);

        _inner.Register(
            widget);
    }

    public void RegisterOrUpdateProfile(
        WidgetWorkloadProfile profile)
    {
        _policyGate.RegisterOrUpdateProfile(
            profile);
    }

    public WidgetExecutionPolicyResult Evaluate(
        string widgetId,
        WidgetPolicyWorkKind workKind)
    {
        return _policyGate.Evaluate(
            widgetId,
            workKind);
    }

    public WidgetRuntimeSnapshot GetSnapshot(
        string widgetId)
    {
        return _inner.GetSnapshot(
            widgetId);
    }

    public Task InitializeAsync(
        string widgetId,
        IWidgetContext context,
        CancellationToken cancellationToken)
    {
        return _inner.InitializeAsync(
            widgetId,
            context,
            cancellationToken);
    }

    public Task StartAsync(
        string widgetId,
        CancellationToken cancellationToken)
    {
        EnsureAllowed(
            widgetId,
            WidgetPolicyWorkKind.General);

        return _inner.StartAsync(
            widgetId,
            cancellationToken);
    }

    public Task PauseAsync(
        string widgetId,
        CancellationToken cancellationToken)
    {
        return _inner.PauseAsync(
            widgetId,
            cancellationToken);
    }

    public Task ResumeAsync(
        string widgetId,
        CancellationToken cancellationToken)
    {
        EnsureAllowed(
            widgetId,
            WidgetPolicyWorkKind.General);

        return _inner.ResumeAsync(
            widgetId,
            cancellationToken);
    }

    public Task StopAsync(
        string widgetId,
        CancellationToken cancellationToken)
    {
        return _inner.StopAsync(
            widgetId,
            cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _inner.DisposeAsync();
    }

    private void EnsureAllowed(
        string widgetId,
        WidgetPolicyWorkKind workKind)
    {
        var decision =
            _policyGate.Evaluate(
                widgetId,
                workKind);

        if (!decision.IsAllowed)
        {
            throw new WidgetPolicySuppressedException(
                widgetId,
                workKind,
                decision);
        }
    }
}

public sealed record PolicyAwareScheduledJobSnapshot(
    string JobId,
    long ExecutedCycles,
    long SuppressedCycles,
    WidgetExecutionPolicyResult? LastDecision);

public sealed class PolicyAwarePeriodicWidgetScheduler
    : IWidgetScheduler, IAsyncDisposable
{
    private readonly ConcurrentDictionary<
        string,
        ScheduledJob> _jobs =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly IWidgetExecutionPolicyGate _policyGate;

    private readonly string _widgetId;

    private readonly WidgetPolicyWorkKind _defaultWorkKind;

    public PolicyAwarePeriodicWidgetScheduler(
        IWidgetExecutionPolicyGate policyGate,
        string widgetId,
        WidgetPolicyWorkKind defaultWorkKind =
            WidgetPolicyWorkKind.General)
    {
        ArgumentNullException.ThrowIfNull(
            policyGate);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        _policyGate =
            policyGate;

        _widgetId =
            widgetId;

        _defaultWorkKind =
            defaultWorkKind;
    }

    public Task ScheduleAsync(
        string jobId,
        TimeSpan interval,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        return ScheduleAsync(
            jobId,
            interval,
            _defaultWorkKind,
            action,
            cancellationToken);
    }

    public Task ScheduleAsync(
        string jobId,
        TimeSpan interval,
        WidgetPolicyWorkKind workKind,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            jobId);

        ArgumentNullException.ThrowIfNull(
            action);

        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                "Interval must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var source =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        var job =
            new ScheduledJob(
                source);

        if (!_jobs.TryAdd(
            jobId,
            job))
        {
            source.Dispose();

            throw new InvalidOperationException(
                $"Scheduled job already exists: {jobId}");
        }

        job.Task =
            Task.Run(
                () =>
                    RunPeriodicAsync(
                        job,
                        interval,
                        workKind,
                        action,
                        source.Token),
                CancellationToken.None);

        return Task.CompletedTask;
    }

    public PolicyAwareScheduledJobSnapshot GetSnapshot(
        string jobId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            jobId);

        return _jobs.TryGetValue(
            jobId,
            out var job)
            ? job.ToSnapshot(
                jobId)
            : throw new KeyNotFoundException(
                $"Scheduled job not found: {jobId}");
    }

    public async Task CancelAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            jobId);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_jobs.TryRemove(
            jobId,
            out var job))
        {
            return;
        }

        job.Source.Cancel();

        try
        {
            await job.Task;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            job.Source.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var jobId in
            _jobs.Keys.ToArray())
        {
            await CancelAsync(
                jobId,
                CancellationToken.None);
        }
    }

    private async Task RunPeriodicAsync(
        ScheduledJob job,
        TimeSpan interval,
        WidgetPolicyWorkKind workKind,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var delayDecision =
                _policyGate.Evaluate(
                    _widgetId,
                    workKind);

            job.RecordDecision(
                delayDecision);

            await Task.Delay(
                ScaleInterval(
                    interval,
                    delayDecision.RefreshIntervalMultiplier),
                cancellationToken);

            var executionDecision =
                _policyGate.Evaluate(
                    _widgetId,
                    workKind);

            job.RecordDecision(
                executionDecision);

            if (!executionDecision.IsAllowed)
            {
                job.RecordSuppressed();

                continue;
            }

            await action(
                cancellationToken);

            job.RecordExecuted();
        }
    }

    private static TimeSpan ScaleInterval(
        TimeSpan interval,
        double multiplier)
    {
        if (
            double.IsNaN(
                multiplier)
            || double.IsInfinity(
                multiplier)
            || multiplier <= 0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(multiplier),
                "Refresh-interval multiplier must be finite and greater than zero.");
        }

        var ticks =
            interval.Ticks
            * multiplier;

        if (ticks >= TimeSpan.MaxValue.Ticks)
        {
            return TimeSpan.MaxValue;
        }

        return TimeSpan.FromTicks(
            Math.Max(
                1,
                (long)Math.Ceiling(
                    ticks)));
    }

    private sealed class ScheduledJob
    {
        private long _executedCycles;

        private long _suppressedCycles;

        private WidgetExecutionPolicyResult? _lastDecision;

        public ScheduledJob(
            CancellationTokenSource source)
        {
            Source =
                source;
        }

        public CancellationTokenSource Source { get; }

        public Task Task { get; set; } =
            Task.CompletedTask;

        public void RecordExecuted()
        {
            Interlocked.Increment(
                ref _executedCycles);
        }

        public void RecordSuppressed()
        {
            Interlocked.Increment(
                ref _suppressedCycles);
        }

        public void RecordDecision(
            WidgetExecutionPolicyResult decision)
        {
            ArgumentNullException.ThrowIfNull(
                decision);

            Volatile.Write(
                ref _lastDecision,
                decision);
        }

        public PolicyAwareScheduledJobSnapshot ToSnapshot(
            string jobId)
        {
            return new PolicyAwareScheduledJobSnapshot(
                JobId:
                    jobId,
                ExecutedCycles:
                    Interlocked.Read(
                        ref _executedCycles),
                SuppressedCycles:
                    Interlocked.Read(
                        ref _suppressedCycles),
                LastDecision:
                    Volatile.Read(
                        ref _lastDecision));
        }
    }
}