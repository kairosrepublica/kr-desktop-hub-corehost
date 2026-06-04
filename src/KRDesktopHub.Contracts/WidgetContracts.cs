namespace KRDesktopHub.Contracts;

public enum WidgetActivationMode
{
    AlwaysOn,
    OnDemand,
    ScheduledWindow,
    PeriodicRun,
    EventTriggered,
    ManualOnly
}

public enum WidgetRuntimeState
{
    Discovered,
    Disabled,
    ScheduledInactive,
    Starting,
    Running,
    Paused,
    Stopping,
    Failed,
    Quarantined
}

public enum WidgetHealthStatus
{
    Healthy,
    Degraded,
    Failed,
    Quarantined
}

public enum NotificationPriority
{
    Informational,
    Normal,
    Important,
    Urgent
}

public enum SettingValueType
{
    Boolean,
    Integer,
    Decimal,
    String,
    Enum,
    TimeSpan,
    Path,
    Secret
}

public sealed record WidgetDescriptor(
    string WidgetId,
    string DisplayName,
    Version WidgetVersion,
    Version RequiredContractsVersion,
    Version MinimumHostVersion,
    IReadOnlyList<string> Capabilities);

public sealed record WidgetCommand(
    string CommandId,
    string DisplayName,
    string Description);

public sealed record WidgetHealthResult(
    WidgetHealthStatus Status,
    string Summary,
    DateTimeOffset CheckedAtUtc);

public sealed record NotificationAction(
    string ActionId,
    string DisplayName);

public sealed record WidgetNotification(
    string NotificationId,
    string Title,
    string Message,
    NotificationPriority Priority,
    IReadOnlyList<NotificationAction> Actions);

public sealed record SettingDescriptor(
    string Key,
    string DisplayNameLocalizationKey,
    string DescriptionLocalizationKey,
    object? RecommendedDefault,
    string RecommendedDefaultReasonLocalizationKey,
    SettingValueType ValueType,
    IReadOnlyList<object>? AllowedValues,
    bool RequiresRestart,
    bool IsAdvanced);

public interface IKrWidget
{
    WidgetDescriptor Descriptor { get; }

    Task InitializeAsync(
        IWidgetContext context,
        CancellationToken cancellationToken);

    Task StartAsync(CancellationToken cancellationToken);

    Task PauseAsync(CancellationToken cancellationToken);

    Task ResumeAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

public interface IWidgetContext
{
    IWidgetLogger Logger { get; }

    IWidgetScheduler Scheduler { get; }

    IWidgetStateStore StateStore { get; }

    IWidgetSettingsStore SettingsStore { get; }

    IEventBus EventBus { get; }

    ICommandRegistry Commands { get; }

    IClock Clock { get; }

    ILocalizationService Localization { get; }

    IWidgetNotificationClient Notifications { get; }
}

public interface IWidgetLogger
{
    void Information(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);
}

public interface IWidgetScheduler
{
    Task ScheduleAsync(
        string jobId,
        TimeSpan interval,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken);

    Task CancelAsync(
        string jobId,
        CancellationToken cancellationToken);
}

public interface IWidgetStateStore
{
    Task<T?> ReadAsync<T>(
        string key,
        CancellationToken cancellationToken);

    Task WriteAsync<T>(
        string key,
        T value,
        CancellationToken cancellationToken);
}

public interface IWidgetSettingsStore
{
    Task<T> GetAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken);
}

public interface IEventBus
{
    Task PublishAsync<TEvent>(
        TEvent eventPayload,
        CancellationToken cancellationToken);

    IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, Task> handler);
}

public interface ICommandRegistry
{
    void Register(
        WidgetCommand command,
        Func<CancellationToken, Task> handler);

    Task ExecuteAsync(
        string commandId,
        CancellationToken cancellationToken);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface ILocalizationService
{
    string Get(string key);

    string Get(string key, params object[] arguments);

    string CurrentCultureName { get; }

    Task SetCultureAsync(
        string cultureName,
        CancellationToken cancellationToken);
}

public interface IWidgetNotificationClient
{
    Task PublishAsync(
        WidgetNotification notification,
        CancellationToken cancellationToken);
}

public interface IWidgetHealthCheck
{
    Task<WidgetHealthResult> CheckAsync(
        CancellationToken cancellationToken);
}

public interface ICommandProvider
{
    IEnumerable<WidgetCommand> GetCommands();
}