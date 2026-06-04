using KRDesktopHub.Contracts;

namespace KRDesktopHub.Platform.Abstractions;

public sealed record TrayStatus(
    string Tooltip,
    string? BadgeText = null,
    string? VisualState = null);

public sealed record HotkeyRegistration(
    string CommandId,
    string Gesture);

public sealed record SystemNotification(
    string NotificationId,
    string Title,
    string Message,
    NotificationPriority Priority,
    IReadOnlyList<NotificationAction> Actions);

public sealed record StartupRegistration(
    bool Enabled,
    TimeSpan Delay);

public sealed record PanelPosition(
    double Left,
    double Top,
    string? MonitorId);

public enum PowerState
{
    Unknown,
    AcPower,
    Battery,
    BatterySaver,
    Suspending,
    Resumed
}

public sealed record PowerStateChanged(
    PowerState State,
    DateTimeOffset ChangedAtUtc);

public sealed record NetworkStateChanged(
    bool IsAvailable,
    DateTimeOffset ChangedAtUtc);

public interface ITrayService
{
    Task InitializeAsync(
        CancellationToken cancellationToken);

    Task SetStatusAsync(
        TrayStatus status,
        CancellationToken cancellationToken);

    Task DisposeAsync();
}

public interface IGlobalHotkeyService
{
    Task RegisterAsync(
        HotkeyRegistration registration,
        CancellationToken cancellationToken);

    Task UnregisterAllAsync(
        CancellationToken cancellationToken);
}

public interface ISystemNotificationService
{
    Task PublishAsync(
        SystemNotification notification,
        CancellationToken cancellationToken);
}

public interface IStartupRegistrationService
{
    Task<StartupRegistration> GetAsync(
        CancellationToken cancellationToken);

    Task SetAsync(
        StartupRegistration registration,
        CancellationToken cancellationToken);
}

public interface IPanelWindowService
{
    Task ShowAsync(
        CancellationToken cancellationToken);

    Task HideAsync(
        CancellationToken cancellationToken);

    Task<PanelPosition?> GetLastPositionAsync(
        CancellationToken cancellationToken);

    Task SetPositionAsync(
        PanelPosition position,
        CancellationToken cancellationToken);
}

public interface IPowerStateService
{
    event EventHandler<PowerStateChanged>? Changed;

    PowerState Current { get; }
}

public interface INetworkStateService
{
    event EventHandler<NetworkStateChanged>? Changed;

    bool IsAvailable { get; }
}

public interface IPrivilegeService
{
    bool IsElevated { get; }

    Task<bool> RequestElevationAsync(
        string reason,
        CancellationToken cancellationToken);
}

public interface IPlatformInfoService
{
    string OperatingSystem { get; }

    string Architecture { get; }

    string RuntimeVersion { get; }
}