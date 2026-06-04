using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using KRDesktopHub.Platform.Abstractions;
using Forms = System.Windows.Forms;
using HubPowerState = KRDesktopHub.Platform.Abstractions.PowerState;

namespace KRDesktopHub.Platform.Windows;

public sealed class WindowsPowerStateService
    : IPowerStateService, IDisposable
{
    public WindowsPowerStateService()
    {
        Current = DetectCurrent();
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    public event EventHandler<PowerStateChanged>? Changed;

    public HubPowerState Current { get; private set; }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }

    private void OnPowerModeChanged(
        object? sender,
        PowerModeChangedEventArgs e)
    {
        Current =
            e.Mode switch
            {
                PowerModes.Suspend =>
                    HubPowerState.Suspending,

                PowerModes.Resume =>
                    HubPowerState.Resumed,

                _ =>
                    DetectCurrent()
            };

        Changed?.Invoke(
            this,
            new PowerStateChanged(
                Current,
                DateTimeOffset.UtcNow));

        if (e.Mode == PowerModes.Resume)
        {
            Current = DetectCurrent();
        }
    }

    private static HubPowerState DetectCurrent()
    {
        var status =
            Forms.SystemInformation.PowerStatus;

        if (status.PowerLineStatus ==
            Forms.PowerLineStatus.Online)
        {
            return HubPowerState.AcPower;
        }

        if (status.BatteryChargeStatus.HasFlag(
                Forms.BatteryChargeStatus.Low)
            || status.BatteryChargeStatus.HasFlag(
                Forms.BatteryChargeStatus.Critical))
        {
            return HubPowerState.BatterySaver;
        }

        return status.PowerLineStatus ==
            Forms.PowerLineStatus.Offline
                ? HubPowerState.Battery
                : HubPowerState.Unknown;
    }
}

public sealed class WindowsNetworkStateService
    : INetworkStateService, IDisposable
{
    public WindowsNetworkStateService()
    {
        IsAvailable =
            NetworkInterface.GetIsNetworkAvailable();

        NetworkChange.NetworkAvailabilityChanged +=
            OnNetworkAvailabilityChanged;
    }

    public event EventHandler<NetworkStateChanged>? Changed;

    public bool IsAvailable { get; private set; }

    public void Dispose()
    {
        NetworkChange.NetworkAvailabilityChanged -=
            OnNetworkAvailabilityChanged;
    }

    private void OnNetworkAvailabilityChanged(
        object? sender,
        NetworkAvailabilityEventArgs e)
    {
        IsAvailable =
            e.IsAvailable;

        Changed?.Invoke(
            this,
            new NetworkStateChanged(
                IsAvailable,
                DateTimeOffset.UtcNow));
    }
}

public sealed class WindowsSessionStateService
    : ISessionStateService
{
    public WindowsSessionStateService()
    {
        Current =
            SessionState.Unknown;

        SystemEvents.SessionSwitch +=
            OnSessionSwitch;
    }

    public event EventHandler<SessionStateChanged>? Changed;

    public SessionState Current { get; private set; }

    public void Dispose()
    {
        SystemEvents.SessionSwitch -=
            OnSessionSwitch;
    }

    private void OnSessionSwitch(
        object? sender,
        SessionSwitchEventArgs e)
    {
        Current =
            e.Reason switch
            {
                SessionSwitchReason.SessionLock =>
                    SessionState.Locked,

                SessionSwitchReason.SessionUnlock =>
                    SessionState.Unlocked,

                _ =>
                    Current
            };

        Changed?.Invoke(
            this,
            new SessionStateChanged(
                Current,
                DateTimeOffset.UtcNow));
    }
}

public sealed class WindowsTimeZoneChangeService
    : ITimeZoneChangeService
{
    public WindowsTimeZoneChangeService()
    {
        CurrentTimeZoneId =
            TimeZoneInfo.Local.Id;

        SystemEvents.TimeChanged +=
            OnTimeChanged;
    }

    public event EventHandler<TimeZoneChanged>? Changed;

    public string CurrentTimeZoneId { get; private set; }

    public void Dispose()
    {
        SystemEvents.TimeChanged -=
            OnTimeChanged;
    }

    private void OnTimeChanged(
        object? sender,
        EventArgs e)
    {
        var previous =
            CurrentTimeZoneId;

        TimeZoneInfo.ClearCachedData();

        var current =
            TimeZoneInfo.Local.Id;

        if (string.Equals(
            previous,
            current,
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentTimeZoneId =
            current;

        Changed?.Invoke(
            this,
            new TimeZoneChanged(
                previous,
                current,
                DateTimeOffset.UtcNow));
    }
}

public sealed class WindowsProcessResourceMonitorService
    : IResourceMonitorService
{
    private readonly TimeSpan _interval;
    private readonly Process _process;
    private CancellationTokenSource? _source;
    private Task? _monitorTask;

    public WindowsProcessResourceMonitorService(
        TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval));
        }

        _interval =
            interval;

        _process =
            Process.GetCurrentProcess();
    }

    public event EventHandler<ResourceSample>? Sampled;

    public ResourceSample? Latest { get; private set; }

    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_monitorTask is not null)
        {
            return Task.CompletedTask;
        }

        _source =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        _monitorTask =
            Task.Run(
                () =>
                    MonitorAsync(
                        _source.Token),

                CancellationToken.None);

        return Task.CompletedTask;
    }

    public async Task StopAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var source =
            _source;

        var monitorTask =
            _monitorTask;

        _source =
            null;

        _monitorTask =
            null;

        if (source is null
            || monitorTask is null)
        {
            return;
        }

        source.Cancel();

        try
        {
            await monitorTask;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            source.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(
            CancellationToken.None);

        _process.Dispose();
    }

    private async Task MonitorAsync(
        CancellationToken cancellationToken)
    {
        _process.Refresh();

        var previousProcessorTime =
            _process.TotalProcessorTime;

        var previousSampleTime =
            DateTimeOffset.UtcNow;

        using var timer =
            new PeriodicTimer(
                _interval);

        while (await timer.WaitForNextTickAsync(
            cancellationToken))
        {
            _process.Refresh();

            var sampledAtUtc =
                DateTimeOffset.UtcNow;

            var processorTime =
                _process.TotalProcessorTime;

            var elapsed =
                sampledAtUtc - previousSampleTime;

            var processorDelta =
                processorTime - previousProcessorTime;

            var cpuPercent =
                elapsed <= TimeSpan.Zero
                    ? 0
                    : processorDelta.TotalMilliseconds
                        / elapsed.TotalMilliseconds
                        / Environment.ProcessorCount
                        * 100;

            var sample =
                new ResourceSample(
                    sampledAtUtc,
                    Math.Clamp(
                        cpuPercent,
                        0,
                        100),

                    _process.WorkingSet64);

            Latest =
                sample;

            Sampled?.Invoke(
                this,
                sample);

            previousProcessorTime =
                processorTime;

            previousSampleTime =
                sampledAtUtc;
        }
    }
}