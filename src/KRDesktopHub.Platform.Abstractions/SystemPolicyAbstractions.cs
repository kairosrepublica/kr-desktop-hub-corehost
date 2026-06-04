namespace KRDesktopHub.Platform.Abstractions;

public enum SessionState
{
    Unknown,
    Locked,
    Unlocked
}

public sealed record SessionStateChanged(
    SessionState State,
    DateTimeOffset ChangedAtUtc);

public sealed record TimeZoneChanged(
    string PreviousTimeZoneId,
    string CurrentTimeZoneId,
    DateTimeOffset ChangedAtUtc);

public sealed record ResourceSample(
    DateTimeOffset SampledAtUtc,
    double CpuPercent,
    long WorkingSetBytes);

public interface ISessionStateService
    : IDisposable
{
    event EventHandler<SessionStateChanged>? Changed;

    SessionState Current { get; }
}

public interface ITimeZoneChangeService
    : IDisposable
{
    event EventHandler<TimeZoneChanged>? Changed;

    string CurrentTimeZoneId { get; }
}

public interface IResourceMonitorService
    : IAsyncDisposable
{
    event EventHandler<ResourceSample>? Sampled;

    ResourceSample? Latest { get; }

    Task StartAsync(
        CancellationToken cancellationToken);

    Task StopAsync(
        CancellationToken cancellationToken);
}