using KRDesktopHub.Contracts;
using KRDesktopHub.Platform.Abstractions;

namespace KRDesktopHub.Core;

public enum NotificationDeliveryDisposition
{
    Delivered,
    SuppressedDisabled,
    SuppressedQuietHours,
    SuppressedDuplicate,
    SuppressedRateLimit
}

public sealed record NotificationGovernanceOptions(
    bool NotificationsEnabled,
    bool SoundsEnabled,
    int NormalNotificationLimitPerTenMinutes,
    bool MergeDuplicateNotifications,
    TimeSpan DuplicateNotificationMergeWindow,
    bool QuietHoursEnabled,
    TimeOnly QuietHoursStartLocal,
    TimeOnly QuietHoursEndLocal)
{
    public void Validate()
    {
        if (
            NormalNotificationLimitPerTenMinutes
            is < 0 or > 120
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    NormalNotificationLimitPerTenMinutes));
        }

        if (DuplicateNotificationMergeWindow < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    DuplicateNotificationMergeWindow));
        }
    }
}

public sealed record NotificationDeliveryResult(
    NotificationDeliveryDisposition Disposition,
    bool SoundAllowed,
    DateTimeOffset EvaluatedAtUtc)
{
    public bool Delivered =>
        Disposition ==
            NotificationDeliveryDisposition.Delivered;
}

public sealed class GovernedSystemNotificationService
{
    private static readonly TimeSpan OrdinaryRateLimitWindow =
        TimeSpan.FromMinutes(
            10);

    private readonly ISystemNotificationService _inner;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate =
        new(
            1,
            1);

    private readonly Queue<DateTimeOffset> _ordinaryDeliveryTimes =
        new();

    private readonly Dictionary<string, DateTimeOffset> _lastOrdinaryDeliveryById =
        new(
            StringComparer.OrdinalIgnoreCase);

    private NotificationGovernanceOptions _options;

    public GovernedSystemNotificationService(
        ISystemNotificationService inner,
        NotificationGovernanceOptions options,
        TimeProvider? timeProvider =
            null)
    {
        ArgumentNullException.ThrowIfNull(
            inner);

        ArgumentNullException.ThrowIfNull(
            options);

        options.Validate();

        _inner =
            inner;

        _options =
            options;

        _timeProvider =
            timeProvider
            ?? TimeProvider.System;
    }

    public void UpdateOptions(
        NotificationGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        options.Validate();

        _options =
            options;
    }

    public async Task<NotificationDeliveryResult> PublishAsync(
        SystemNotification notification,
        bool force,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            notification);

        await _gate.WaitAsync(
            cancellationToken);

        try
        {
            var evaluatedAtUtc =
                _timeProvider.GetUtcNow();

            var options =
                _options;

            var disposition =
                EvaluateDisposition(
                    notification,
                    force,
                    evaluatedAtUtc,
                    options);

            var result =
                new NotificationDeliveryResult(
                    disposition,
                    SoundAllowed:
                        options.SoundsEnabled,
                    EvaluatedAtUtc:
                        evaluatedAtUtc);

            if (!result.Delivered)
            {
                return result;
            }

            await _inner.PublishAsync(
                notification,
                cancellationToken);

            if (!force)
            {
                RecordDelivered(
                    notification,
                    evaluatedAtUtc);
            }

            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private NotificationDeliveryDisposition EvaluateDisposition(
        SystemNotification notification,
        bool force,
        DateTimeOffset evaluatedAtUtc,
        NotificationGovernanceOptions options)
    {
        if (force)
        {
            return NotificationDeliveryDisposition.Delivered;
        }

        if (!options.NotificationsEnabled)
        {
            return NotificationDeliveryDisposition.SuppressedDisabled;
        }

        var isOrdinary =
            notification.Priority
            is NotificationPriority.Informational
            or NotificationPriority.Normal;

        if (!isOrdinary)
        {
            return NotificationDeliveryDisposition.Delivered;
        }

        if (
            options.QuietHoursEnabled
            && IsWithinQuietHours(
                TimeOnly.FromDateTime(
                    _timeProvider
                        .GetLocalNow()
                        .DateTime),

                options.QuietHoursStartLocal,
                options.QuietHoursEndLocal)
        )
        {
            return NotificationDeliveryDisposition.SuppressedQuietHours;
        }

        PruneOrdinaryHistory(
            evaluatedAtUtc,
            options);

        if (
            options.MergeDuplicateNotifications
            && _lastOrdinaryDeliveryById.TryGetValue(
                notification.NotificationId,
                out var lastDeliveredAtUtc)
            && evaluatedAtUtc
                - lastDeliveredAtUtc
                < options.DuplicateNotificationMergeWindow
        )
        {
            return NotificationDeliveryDisposition.SuppressedDuplicate;
        }

        if (
            _ordinaryDeliveryTimes.Count
            >= options.NormalNotificationLimitPerTenMinutes
        )
        {
            return NotificationDeliveryDisposition.SuppressedRateLimit;
        }

        return NotificationDeliveryDisposition.Delivered;
    }

    private void RecordDelivered(
        SystemNotification notification,
        DateTimeOffset deliveredAtUtc)
    {
        if (
            notification.Priority
            is not (
                NotificationPriority.Informational
                or NotificationPriority.Normal
            )
        )
        {
            return;
        }

        _ordinaryDeliveryTimes.Enqueue(
            deliveredAtUtc);

        _lastOrdinaryDeliveryById[notification.NotificationId] =
            deliveredAtUtc;
    }

    private void PruneOrdinaryHistory(
        DateTimeOffset evaluatedAtUtc,
        NotificationGovernanceOptions options)
    {
        while (
            _ordinaryDeliveryTimes.Count > 0
            && evaluatedAtUtc
                - _ordinaryDeliveryTimes.Peek()
                >= OrdinaryRateLimitWindow
        )
        {
            _ordinaryDeliveryTimes.Dequeue();
        }

        foreach (var notificationId in _lastOrdinaryDeliveryById
            .Where(
                pair =>
                    evaluatedAtUtc
                    - pair.Value
                    >= options.DuplicateNotificationMergeWindow)
            .Select(
                pair =>
                    pair.Key)
            .ToArray())
        {
            _lastOrdinaryDeliveryById.Remove(
                notificationId);
        }
    }

    private static bool IsWithinQuietHours(
        TimeOnly now,
        TimeOnly start,
        TimeOnly end)
    {
        if (start == end)
        {
            return false;
        }

        return start < end
            ? now >= start
                && now < end
            : now >= start
                || now < end;
    }
}