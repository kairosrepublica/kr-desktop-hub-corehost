using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KRDesktopHub.Contracts;

namespace KRDesktopHub.Core;

public static class WidgetCapabilityCatalog
{
    private static readonly IReadOnlyDictionary<
        string,
        WidgetCapabilityDefinition> Definitions =
            new Dictionary<
                string,
                WidgetCapabilityDefinition>(
                    StringComparer.OrdinalIgnoreCase)
            {
                [WidgetCapabilityIds.ClockRead] =
                    new WidgetCapabilityDefinition(
                        WidgetCapabilityIds.ClockRead,
                        WidgetCapabilityDisposition.Brokered,
                        "Read the current local time and local time-zone information through CoreHost."),

                [WidgetCapabilityIds.NotificationSend] =
                    new WidgetCapabilityDefinition(
                        WidgetCapabilityIds.NotificationSend,
                        WidgetCapabilityDisposition.Brokered,
                        "Request a governed notification through CoreHost."),

                [WidgetCapabilityIds.NetworkHttp] =
                    new WidgetCapabilityDefinition(
                        WidgetCapabilityIds.NetworkHttp,
                        WidgetCapabilityDisposition.Reserved,
                        "Reserved future HTTP broker. Not enabled in the current CoreHost release."),

                [WidgetCapabilityIds.CalendarRead] =
                    new WidgetCapabilityDefinition(
                        WidgetCapabilityIds.CalendarRead,
                        WidgetCapabilityDisposition.Reserved,
                        "Reserved future calendar broker. Not enabled in the current CoreHost release."),

                [WidgetCapabilityIds.FileReadScoped] =
                    new WidgetCapabilityDefinition(
                        WidgetCapabilityIds.FileReadScoped,
                        WidgetCapabilityDisposition.Reserved,
                        "Reserved future scoped file-read broker. Not enabled in the current CoreHost release."),

                [WidgetCapabilityIds.FileWriteScoped] =
                    new WidgetCapabilityDefinition(
                        WidgetCapabilityIds.FileWriteScoped,
                        WidgetCapabilityDisposition.Reserved,
                        "Reserved future scoped file-write broker. Not enabled in the current CoreHost release."),

                [WidgetCapabilityIds.ShellExecute] =
                    new WidgetCapabilityDefinition(
                        WidgetCapabilityIds.ShellExecute,
                        WidgetCapabilityDisposition.Prohibited,
                        "Arbitrary shell execution is prohibited."),

                [WidgetCapabilityIds.ScriptExecute] =
                    new WidgetCapabilityDefinition(
                        WidgetCapabilityIds.ScriptExecute,
                        WidgetCapabilityDisposition.Prohibited,
                        "Arbitrary external-script execution is prohibited.")
            };

    public static IReadOnlyCollection<
        WidgetCapabilityDefinition> All =>
            Definitions
                .Values
                .ToArray();

    public static bool TryGet(
        string capabilityId,
        out WidgetCapabilityDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(
            capabilityId))
        {
            definition =
                null!;

            return false;
        }

        return Definitions.TryGetValue(
            capabilityId,
            out definition!);
    }

    public static bool IsPackageApprovable(
        string capabilityId)
    {
        return TryGet(
                capabilityId,
                out var definition)
            && definition.Disposition
                == WidgetCapabilityDisposition.Brokered;
    }
}

public sealed class InMemoryWidgetCapabilityApprovalStore
    : IWidgetCapabilityApprovalStore
{
    private readonly ConcurrentDictionary<
        string,
        IReadOnlySet<string>> _approvedCapabilities =
            new(
                StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> GetApprovedCapabilities(
        string widgetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        if (_approvedCapabilities.TryGetValue(
            widgetId,
            out var approvedCapabilities))
        {
            return new HashSet<string>(
                approvedCapabilities,
                StringComparer.OrdinalIgnoreCase);
        }

        return new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
    }

    public void SetApprovedCapabilities(
        string widgetId,
        IEnumerable<string> capabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        ArgumentNullException.ThrowIfNull(
            capabilities);

        var approvedCapabilities =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var capability in
            capabilities)
        {
            if (!WidgetCapabilityCatalog.TryGet(
                capability,
                out var definition))
            {
                throw new InvalidOperationException(
                    $"Unknown Widget capability cannot be approved: {capability}");
            }

            if (
                definition.Disposition
                != WidgetCapabilityDisposition.Brokered
            )
            {
                throw new InvalidOperationException(
                    $"Widget capability cannot be approved in the current CoreHost release: {capability}");
            }

            approvedCapabilities.Add(
                capability);
        }

        _approvedCapabilities[widgetId] =
            approvedCapabilities;
    }
}

public sealed class InMemoryWidgetCapabilityAuditSink
    : IWidgetCapabilityAuditSink
{
    private readonly ConcurrentQueue<
        WidgetCapabilityAuditRecord> _records =
            new();

    public void Record(
        WidgetCapabilityAuditRecord record)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        _records.Enqueue(
            record);
    }

    public IReadOnlyList<
        WidgetCapabilityAuditRecord> Snapshot()
    {
        return _records.ToArray();
    }
}

public sealed class DefaultWidgetCapabilityAuthorizer
    : IWidgetCapabilityAuthorizer
{
    private readonly IWidgetCapabilityApprovalStore _approvalStore;

    private readonly IWidgetCapabilityAuditSink _auditSink;

    public DefaultWidgetCapabilityAuthorizer(
        IWidgetCapabilityApprovalStore approvalStore,
        IWidgetCapabilityAuditSink auditSink)
    {
        ArgumentNullException.ThrowIfNull(
            approvalStore);

        ArgumentNullException.ThrowIfNull(
            auditSink);

        _approvalStore =
            approvalStore;

        _auditSink =
            auditSink;
    }

    public WidgetCapabilityDecision Authorize(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        string capabilityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        ArgumentNullException.ThrowIfNull(
            declaredCapabilities);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            capabilityId);

        if (!WidgetCapabilityCatalog.TryGet(
            capabilityId,
            out var definition))
        {
            return Record(
                widgetId,
                capabilityId,
                WidgetCapabilityDecisionCode.UnknownCapability,
                "Capability is unknown to CoreHost.");
        }

        if (
            definition.Disposition
            == WidgetCapabilityDisposition.Prohibited
        )
        {
            return Record(
                widgetId,
                capabilityId,
                WidgetCapabilityDecisionCode.ProhibitedCapability,
                "Capability is prohibited by CoreHost.");
        }

        if (
            definition.Disposition
            == WidgetCapabilityDisposition.Reserved
        )
        {
            return Record(
                widgetId,
                capabilityId,
                WidgetCapabilityDecisionCode.ReservedCapabilityUnavailable,
                "Capability is reserved for a future CoreHost release.");
        }

        if (!declaredCapabilities.Contains(
            capabilityId))
        {
            return Record(
                widgetId,
                capabilityId,
                WidgetCapabilityDecisionCode.NotDeclared,
                "Widget package did not declare this capability.");
        }

        var approvedCapabilities =
            _approvalStore.GetApprovedCapabilities(
                widgetId);

        if (!approvedCapabilities.Contains(
            capabilityId))
        {
            return Record(
                widgetId,
                capabilityId,
                WidgetCapabilityDecisionCode.NotApproved,
                "Capability was not approved for this Widget.");
        }

        return Record(
            widgetId,
            capabilityId,
            WidgetCapabilityDecisionCode.Allowed,
            "Capability is declared and approved.");
    }

    private WidgetCapabilityDecision Record(
        string widgetId,
        string capabilityId,
        WidgetCapabilityDecisionCode code,
        string reason)
    {
        var decision =
            new WidgetCapabilityDecision(
                WidgetId:
                    widgetId,
                CapabilityId:
                    capabilityId,
                Code:
                    code,
                Reason:
                    reason);

        _auditSink.Record(
            new WidgetCapabilityAuditRecord(
                TimestampUtc:
                    DateTimeOffset.UtcNow,
                WidgetId:
                    widgetId,
                CapabilityId:
                    capabilityId,
                Code:
                    code,
                Reason:
                    reason));

        return decision;
    }
}

public sealed class WidgetCapabilityDeniedException
    : InvalidOperationException
{
    public WidgetCapabilityDeniedException(
        WidgetCapabilityDecision decision)
        : base(
            CreateMessage(
                decision))
    {
        Decision =
            decision;
    }

    public WidgetCapabilityDecision Decision { get; }

    private static string CreateMessage(
        WidgetCapabilityDecision decision)
    {
        ArgumentNullException.ThrowIfNull(
            decision);

        return $"Widget capability denied: widget={decision.WidgetId}; capability={decision.CapabilityId}; code={decision.Code}; reason={decision.Reason}";
    }
}

public sealed class GovernedWidgetClockBroker
    : IWidgetClockBroker
{
    private readonly IWidgetCapabilityAuthorizer _authorizer;

    public GovernedWidgetClockBroker(
        IWidgetCapabilityAuthorizer authorizer)
    {
        ArgumentNullException.ThrowIfNull(
            authorizer);

        _authorizer =
            authorizer;
    }

    public Task<WidgetLocalClockSnapshot> ReadLocalClockAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        EnsureAllowed(
            _authorizer.Authorize(
                widgetId,
                declaredCapabilities,
                WidgetCapabilityIds.ClockRead));

        var localNow =
            DateTimeOffset.Now;

        return Task.FromResult(
            new WidgetLocalClockSnapshot(
                LocalNow:
                    localNow,
                TimeZoneId:
                    TimeZoneInfo.Local.Id,
                UtcOffset:
                    localNow.Offset));
    }

    private static void EnsureAllowed(
        WidgetCapabilityDecision decision)
    {
        if (!decision.IsAllowed)
        {
            throw new WidgetCapabilityDeniedException(
                decision);
        }
    }
}

public sealed class GovernedWidgetNotificationBroker
    : IWidgetNotificationBroker
{
    private readonly IWidgetCapabilityAuthorizer _authorizer;

    private readonly Func<
        WidgetNotificationBrokerRequest,
        CancellationToken,
        Task> _sender;

    public GovernedWidgetNotificationBroker(
        IWidgetCapabilityAuthorizer authorizer,
        Func<
            WidgetNotificationBrokerRequest,
            CancellationToken,
            Task> sender)
    {
        ArgumentNullException.ThrowIfNull(
            authorizer);

        ArgumentNullException.ThrowIfNull(
            sender);

        _authorizer =
            authorizer;

        _sender =
            sender;
    }

    public Task SendAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        WidgetNotificationBrokerRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(
            request);

        EnsureAllowed(
            _authorizer.Authorize(
                widgetId,
                declaredCapabilities,
                WidgetCapabilityIds.NotificationSend));

        return _sender(
            request,
            cancellationToken);
    }

    private static void EnsureAllowed(
        WidgetCapabilityDecision decision)
    {
        if (!decision.IsAllowed)
        {
            throw new WidgetCapabilityDeniedException(
                decision);
        }
    }
}