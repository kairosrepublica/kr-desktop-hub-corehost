using KRDesktopHub.App.Windows;
using KRDesktopHub.Contracts;
using KRDesktopHub.Core;
using KRDesktopHub.Platform.Abstractions;
using KRDesktopHub.Platform.Windows;

var recommended =
    CoreHostSettingsCatalog.Recommended;

if (recommended.SchemaVersion != 2)
{
    throw new InvalidOperationException(
        "CoreHost settings schema version must be 2.");
}


if (
    CoreHostPanelShellPolicy.ShowActivated
    || CoreHostPanelShellPolicy.ShowInTaskbar
    || CoreHostPanelShellPolicy.ForceActivateAfterOrdinaryShow
)
{
    throw new InvalidOperationException(
        "CoreHost panel shell activation policy must remain non-disruptive.");
}

var shellDiagnostic =
    CoreHostPanelShellDiagnosticFormatter
        .Format(
            new CoreHostPanelShellDiagnosticSnapshot(
                Action:
                    "show",

                Reason:
                    "smoke",

                WasVisible:
                    false,

                IsVisible:
                    true,

                IsActive:
                    false,

                FocusedElementType:
                    "<none>",

                Left:
                    10.5,

                Top:
                    20.5,

                Width:
                    600,

                Height:
                    720,

                WorkAreaLeft:
                    0,

                WorkAreaTop:
                    0,

                WorkAreaWidth:
                    1920,

                WorkAreaHeight:
                    1040,

                Topmost:
                    false,

                ShowActivated:
                    false,

                ShowInTaskbar:
                    false));

if (
    !shellDiagnostic.Contains(
        "action=show",
        StringComparison.Ordinal)
    || !shellDiagnostic.Contains(
        "showActivated=False",
        StringComparison.Ordinal)
    || !shellDiagnostic.Contains(
        "showInTaskbar=False",
        StringComparison.Ordinal)
)
{
    throw new InvalidOperationException(
        "CoreHost panel shell diagnostic formatter validation failed.");
}

var policyOptions =
    CoreHostSettingsRuntimeBindings
        .ToSystemPolicyOptions(
            recommended);

if (
    policyOptions.NetworkRecoveryDebounce !=
        TimeSpan.FromSeconds(
            5)
    || policyOptions.ResourceSampleInterval !=
        TimeSpan.FromSeconds(
            30)
    || !policyOptions.BatteryAwareRefreshThrottling
)
{
    throw new InvalidOperationException(
        "Settings-to-system-policy mapping failed.");
}

var unconstrainedBatterySettings =
    recommended with
    {
        BatteryAwareRefreshThrottling =
            false
    };

var unconstrainedBatteryEvaluator =
    new SystemPolicyEvaluator(
        CoreHostSettingsRuntimeBindings
            .ToSystemPolicyOptions(
                unconstrainedBatterySettings));

var lowPriorityProfile =
    new WidgetWorkloadProfile(
        WidgetId:
            "kr.fixture.low-priority",

        IsActive:
            true,

        IsLowPriority:
            true,

        IsNetworkHeavy:
            false,

        RequiresVisualRefresh:
            true,

        IsTimeSensitive:
            false);

var batteryState =
    new SystemPolicyState(
        PowerState:
            PowerState.Battery,

        IsNetworkAvailable:
            true,

        SessionState:
            SessionState.Unlocked,

        TimeZoneId:
            TimeZoneInfo.Local.Id,

        IsPanelVisible:
            true,

        LastResumeAtUtc:
            null);

var unconstrainedBatteryDecision =
    unconstrainedBatteryEvaluator.Evaluate(
        lowPriorityProfile,
        batteryState);

if (
    !unconstrainedBatteryDecision.AllowExecution
    || unconstrainedBatteryDecision.RefreshIntervalMultiplier !=
        1.0
)
{
    throw new InvalidOperationException(
        "Battery-aware throttling disable binding failed.");
}

var fakeClock =
    new ManualTimeProvider(
        new DateTimeOffset(
            2026,
            6,
            5,
            23,
            30,
            0,
            TimeSpan.Zero));

var sink =
    new RecordingNotificationService();

var quietOptions =
    new NotificationGovernanceOptions(
        NotificationsEnabled:
            true,

        SoundsEnabled:
            false,

        NormalNotificationLimitPerTenMinutes:
            2,

        MergeDuplicateNotifications:
            true,

        DuplicateNotificationMergeWindow:
            TimeSpan.FromMinutes(
                10),

        QuietHoursEnabled:
            true,

        QuietHoursStartLocal:
            new TimeOnly(
                23,
                0),

        QuietHoursEndLocal:
            new TimeOnly(
                8,
                0));

var governor =
    new GovernedSystemNotificationService(
        sink,
        quietOptions,
        fakeClock);

var ordinary =
    new SystemNotification(
        "ordinary.one",
        "Ordinary",
        "Ordinary notification",
        NotificationPriority.Normal,
        Array.Empty<NotificationAction>());

var quietResult =
    await governor.PublishAsync(
        ordinary,
        force:
            false,
        CancellationToken.None);

if (
    quietResult.Disposition !=
        NotificationDeliveryDisposition.SuppressedQuietHours
    || sink.Delivered.Count != 0
)
{
    throw new InvalidOperationException(
        "Quiet-hours suppression failed.");
}

var important =
    new SystemNotification(
        "important.one",
        "Important",
        "Important notification",
        NotificationPriority.Important,
        Array.Empty<NotificationAction>());

var importantResult =
    await governor.PublishAsync(
        important,
        force:
            false,
        CancellationToken.None);

if (
    !importantResult.Delivered
    || sink.Delivered.Count != 1
)
{
    throw new InvalidOperationException(
        "Important-notification quiet-hours bypass failed.");
}

fakeClock.SetUtcNow(
    new DateTimeOffset(
        2026,
        6,
        5,
        12,
        0,
        0,
        TimeSpan.Zero));

governor.UpdateOptions(
    quietOptions with
    {
        QuietHoursEnabled =
            false
    });

var firstOrdinary =
    await governor.PublishAsync(
        ordinary,
        force:
            false,
        CancellationToken.None);

var duplicateOrdinary =
    await governor.PublishAsync(
        ordinary,
        force:
            false,
        CancellationToken.None);

if (
    !firstOrdinary.Delivered
    || duplicateOrdinary.Disposition !=
        NotificationDeliveryDisposition.SuppressedDuplicate
)
{
    throw new InvalidOperationException(
        "Duplicate-notification suppression failed.");
}

var ordinaryTwo =
    new SystemNotification(
        "ordinary.two",
        "Ordinary Two",
        "Ordinary notification two",
        NotificationPriority.Informational,
        Array.Empty<NotificationAction>());

var ordinaryThree =
    new SystemNotification(
        "ordinary.three",
        "Ordinary Three",
        "Ordinary notification three",
        NotificationPriority.Normal,
        Array.Empty<NotificationAction>());

var secondOrdinary =
    await governor.PublishAsync(
        ordinaryTwo,
        force:
            false,
        CancellationToken.None);

var rateLimited =
    await governor.PublishAsync(
        ordinaryThree,
        force:
            false,
        CancellationToken.None);

if (
    !secondOrdinary.Delivered
    || rateLimited.Disposition !=
        NotificationDeliveryDisposition.SuppressedRateLimit
)
{
    throw new InvalidOperationException(
        "Ordinary-notification rate limiting failed.");
}

var forced =
    await governor.PublishAsync(
        ordinaryThree,
        force:
            true,
        CancellationToken.None);

if (!forced.Delivered)
{
    throw new InvalidOperationException(
        "Forced notification delivery failed.");
}

var disabledGovernor =
    new GovernedSystemNotificationService(
        new RecordingNotificationService(),
        quietOptions with
        {
            NotificationsEnabled =
                false,

            QuietHoursEnabled =
                false
        },
        fakeClock);

var disabledResult =
    await disabledGovernor.PublishAsync(
        ordinary,
        force:
            false,
        CancellationToken.None);

var forcedWhenDisabled =
    await disabledGovernor.PublishAsync(
        ordinary,
        force:
            true,
        CancellationToken.None);

if (
    disabledResult.Disposition !=
        NotificationDeliveryDisposition.SuppressedDisabled
    || !forcedWhenDisabled.Delivered
)
{
    throw new InvalidOperationException(
        "Disabled-notification gate or forced safety bypass failed.");
}

if (forced.SoundAllowed)
{
    throw new InvalidOperationException(
        "Notification sound-policy mapping failed.");
}

var coordinator =
    new SystemPolicyCoordinator(
        new InMemoryEventBus(),
        new MutablePowerStateService(
            PowerState.AcPower),
        new MutableNetworkStateService(
            isAvailable:
                true),
        new MutableSessionStateService(
            SessionState.Unlocked),
        new MutableTimeZoneService(
            "Initial/Zone"),
        new PassiveResourceMonitorService(),
        new SystemPolicyEvaluator(
            CoreHostPolicyOptions.Recommended));

coordinator.UpdateOptions(
    CoreHostSettingsRuntimeBindings
        .ToSystemPolicyOptions(
            recommended with
            {
                BatteryAwareRefreshThrottling =
                    false
            }));

var coordinatorDecision =
    coordinator.Evaluate(
        lowPriorityProfile);

if (!coordinatorDecision.AllowExecution)
{
    throw new InvalidOperationException(
        "Coordinator runtime policy update failed.");
}

coordinator.Dispose();

var legacy =
    recommended with
    {
        SchemaVersion =
            1,

        DuplicateNotificationMergeWindowSeconds =
            0,

        QuietHoursEnabled =
            false,

        NetworkRecoveryDebounceSeconds =
            0,

        ResourceSampleIntervalSeconds =
            0
    };

var migrated =
    CoreHostSettingsValidator.Normalize(
        legacy);

if (
    migrated.SchemaVersion != 2
    || migrated.DuplicateNotificationMergeWindowSeconds !=
        600
    || !migrated.QuietHoursEnabled
    || migrated.NetworkRecoveryDebounceSeconds !=
        5
    || migrated.ResourceSampleIntervalSeconds !=
        30
)
{
    throw new InvalidOperationException(
        "Schema v1 to v2 migration failed.");
}

var recommendationNames =
    CoreHostSettingsCatalog
        .Recommendations
        .Select(
            item =>
                item.SettingName)
        .ToHashSet(
            StringComparer.Ordinal);

var editableSettingNames =
    typeof(
        CoreHostSettings)
        .GetProperties()
        .Select(
            property =>
                property.Name)
        .Where(
            name =>
                name !=
                    nameof(
                        CoreHostSettings.SchemaVersion)
                && name !=
                    nameof(
                        CoreHostSettings.SavedAtUtc))
        .ToHashSet(
            StringComparer.Ordinal);

if (!editableSettingNames.SetEquals(
    recommendationNames))
{
    throw new InvalidOperationException(
        "Recommended defaults and reasons must cover every editable setting.");
}

Console.WriteLine(
    "Batch 8B2A Policy Binding smoke test passed.");

public sealed class ManualTimeProvider
    : TimeProvider
{
    private DateTimeOffset _utcNow;

    public ManualTimeProvider(
        DateTimeOffset utcNow)
    {
        _utcNow =
            utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    public override TimeZoneInfo LocalTimeZone =>
        TimeZoneInfo.Utc;

    public void SetUtcNow(
        DateTimeOffset utcNow)
    {
        _utcNow =
            utcNow;
    }
}

public sealed class RecordingNotificationService
    : ISystemNotificationService
{
    public List<SystemNotification> Delivered { get; } =
        new();

    public Task PublishAsync(
        SystemNotification notification,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Delivered.Add(
            notification);

        return Task.CompletedTask;
    }
}

public sealed class MutablePowerStateService
    : IPowerStateService
{
    public MutablePowerStateService(
        PowerState current)
    {
        Current =
            current;
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
        SessionState current)
    {
        Current =
            current;
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