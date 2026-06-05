using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KRDesktopHub.Contracts;

public static class WidgetCapabilityIds
{
    public const string ClockRead =
        "clock.read";

    public const string NotificationSend =
        "notification.send";

    public const string NetworkHttp =
        "network.http";

    public const string CalendarRead =
        "calendar.read";

    public const string FileReadScoped =
        "file.read.scoped";

    public const string FileWriteScoped =
        "file.write.scoped";

    public const string ShellExecute =
        "shell.execute";

    public const string ScriptExecute =
        "script.execute";
}

public enum WidgetCapabilityDisposition
{
    Brokered,
    Reserved,
    Prohibited
}

public sealed record WidgetCapabilityDefinition(
    string Id,
    WidgetCapabilityDisposition Disposition,
    string Description);

public enum WidgetCapabilityDecisionCode
{
    Allowed,
    UnknownCapability,
    ProhibitedCapability,
    ReservedCapabilityUnavailable,
    NotDeclared,
    NotApproved
}

public sealed record WidgetCapabilityDecision(
    string WidgetId,
    string CapabilityId,
    WidgetCapabilityDecisionCode Code,
    string Reason)
{
    public bool IsAllowed =>
        Code
        == WidgetCapabilityDecisionCode.Allowed;
}

public sealed record WidgetCapabilityAuditRecord(
    DateTimeOffset TimestampUtc,
    string WidgetId,
    string CapabilityId,
    WidgetCapabilityDecisionCode Code,
    string Reason);

public interface IWidgetCapabilityApprovalStore
{
    IReadOnlySet<string> GetApprovedCapabilities(
        string widgetId);
}

public interface IWidgetCapabilityAuditSink
{
    void Record(
        WidgetCapabilityAuditRecord record);
}

public interface IWidgetCapabilityAuthorizer
{
    WidgetCapabilityDecision Authorize(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        string capabilityId);
}

public sealed record WidgetLocalClockSnapshot(
    DateTimeOffset LocalNow,
    string TimeZoneId,
    TimeSpan UtcOffset);

public interface IWidgetClockBroker
{
    Task<WidgetLocalClockSnapshot> ReadLocalClockAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        CancellationToken cancellationToken);
}

public sealed record WidgetNotificationBrokerRequest(
    string Title,
    string Body,
    string? ActivationArgument);

public interface IWidgetNotificationBroker
{
    Task SendAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        WidgetNotificationBrokerRequest request,
        CancellationToken cancellationToken);
}

public sealed record WidgetHttpBrokerRequest(
    string Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    byte[]? Body);

public sealed record WidgetHttpBrokerResponse(
    int StatusCode,
    IReadOnlyDictionary<string, string[]> Headers,
    byte[] Body);

public interface IWidgetHttpBroker
{
    Task<WidgetHttpBrokerResponse> SendAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        WidgetHttpBrokerRequest request,
        CancellationToken cancellationToken);
}

public sealed record WidgetScopedFileReadRequest(
    string ScopeId,
    string RelativePath);

public sealed record WidgetScopedFileWriteRequest(
    string ScopeId,
    string RelativePath,
    byte[] Content);

public interface IWidgetScopedFileBroker
{
    Task<byte[]> ReadAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        WidgetScopedFileReadRequest request,
        CancellationToken cancellationToken);

    Task WriteAsync(
        string widgetId,
        IReadOnlySet<string> declaredCapabilities,
        WidgetScopedFileWriteRequest request,
        CancellationToken cancellationToken);
}