using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using KRDesktopHub.Contracts;
using KRDesktopHub.Core;

var tempRoot =
    Path.Combine(
        Path.GetTempPath(),
        "KRDesktopHub-WidgetCapabilityGovernance-"
        + Guid
            .NewGuid()
            .ToString(
                "N"));

Directory.CreateDirectory(
    tempRoot);

try
{
    var widgetId =
        "kr.fixture.capability";

    var declaredCapabilities =
        new HashSet<string>(
            new[]
            {
                WidgetCapabilityIds.ClockRead,
                WidgetCapabilityIds.NotificationSend,
                WidgetCapabilityIds.NetworkHttp,
                WidgetCapabilityIds.ShellExecute
            },
            StringComparer.OrdinalIgnoreCase);

    var approvalStore =
        new InMemoryWidgetCapabilityApprovalStore();

    var auditSink =
        new InMemoryWidgetCapabilityAuditSink();

    var authorizer =
        new DefaultWidgetCapabilityAuthorizer(
            approvalStore,
            auditSink);

    AssertDecision(
        authorizer.Authorize(
            widgetId,
            declaredCapabilities,
            WidgetCapabilityIds.ClockRead),
        WidgetCapabilityDecisionCode.NotApproved,
        "Default-deny approval validation failed.");

    approvalStore.SetApprovedCapabilities(
        widgetId,
        new[]
        {
            WidgetCapabilityIds.ClockRead,
            WidgetCapabilityIds.NotificationSend
        });

    AssertDecision(
        authorizer.Authorize(
            widgetId,
            declaredCapabilities,
            WidgetCapabilityIds.ClockRead),
        WidgetCapabilityDecisionCode.Allowed,
        "Explicit clock approval validation failed.");

    AssertDecision(
        authorizer.Authorize(
            widgetId,
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase),
            WidgetCapabilityIds.ClockRead),
        WidgetCapabilityDecisionCode.NotDeclared,
        "Undeclared capability validation failed.");

    AssertDecision(
        authorizer.Authorize(
            widgetId,
            declaredCapabilities,
            WidgetCapabilityIds.NetworkHttp),
        WidgetCapabilityDecisionCode.ReservedCapabilityUnavailable,
        "Reserved network capability validation failed.");

    AssertDecision(
        authorizer.Authorize(
            widgetId,
            declaredCapabilities,
            WidgetCapabilityIds.ShellExecute),
        WidgetCapabilityDecisionCode.ProhibitedCapability,
        "Prohibited shell capability validation failed.");

    AssertDecision(
        authorizer.Authorize(
            widgetId,
            declaredCapabilities,
            "unknown.capability"),
        WidgetCapabilityDecisionCode.UnknownCapability,
        "Unknown capability validation failed.");

    var clockBroker =
        new GovernedWidgetClockBroker(
            authorizer);

    var beforeClockRead =
        DateTimeOffset.Now;

    var clockSnapshot =
        await clockBroker.ReadLocalClockAsync(
            widgetId,
            declaredCapabilities,
            CancellationToken.None);

    var afterClockRead =
        DateTimeOffset.Now;

    if (
        clockSnapshot.LocalNow
        < beforeClockRead
        || clockSnapshot.LocalNow
        > afterClockRead
        || string.IsNullOrWhiteSpace(
            clockSnapshot.TimeZoneId)
    )
    {
        throw new InvalidOperationException(
            "Governed clock broker validation failed.");
    }

    var notificationCount =
        0;

    var notificationBroker =
        new GovernedWidgetNotificationBroker(
            authorizer,
            (_, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                notificationCount++;

                return Task.CompletedTask;
            });

    await notificationBroker.SendAsync(
        widgetId,
        declaredCapabilities,
        new WidgetNotificationBrokerRequest(
            Title:
                "Fixture notification",
            Body:
                "Fixture body",
            ActivationArgument:
                null),
        CancellationToken.None);

    if (notificationCount
        != 1)
    {
        throw new InvalidOperationException(
            "Governed notification broker allowed-call validation failed.");
    }

    await ExpectCapabilityDeniedAsync(
        () =>
            notificationBroker.SendAsync(
                widgetId,
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase),
                new WidgetNotificationBrokerRequest(
                    Title:
                        "Denied notification",
                    Body:
                        "Denied body",
                    ActivationArgument:
                        null),
                CancellationToken.None),
        WidgetCapabilityDecisionCode.NotDeclared,
        "Governed notification broker denial validation failed.");

    if (notificationCount
        != 1)
    {
        throw new InvalidOperationException(
            "Denied notification request unexpectedly reached the sender.");
    }

    if (
        auditSink
            .Snapshot()
            .Count
        < 8
    )
    {
        throw new InvalidOperationException(
            "Capability audit-record validation failed.");
    }

    var installerDataRoot =
        Path.Combine(
            tempRoot,
            "installer-data");

    var installerSourceRoot =
        Path.Combine(
            tempRoot,
            "installer-source");

    Directory.CreateDirectory(
        installerSourceRoot);

    var installer =
        new InternalWidgetPackageInstaller(
            WidgetPackageInstallerOptions.CreateRecommended(
                installerDataRoot,
                new Version(
                    0,
                    1,
                    0),
                new[]
                {
                    WidgetCapabilityIds.ClockRead,
                    WidgetCapabilityIds.ShellExecute,
                    WidgetCapabilityIds.NetworkHttp
                }));

    var clockArchive =
        Path.Combine(
            installerSourceRoot,
            "kr.fixture.clock.krwidget.zip");

    CreateWidgetArchive(
        clockArchive,
        widgetId:
            "kr.fixture.clock",
        capabilities:
            new[]
            {
                WidgetCapabilityIds.ClockRead
            });

    var clockInstall =
        await installer.InstallArchiveAsync(
            clockArchive,
            CancellationToken.None);

    if (
        clockInstall.WidgetId
        != "kr.fixture.clock"
    )
    {
        throw new InvalidOperationException(
            "Approvable brokered capability package installation failed.");
    }

    var shellArchive =
        Path.Combine(
            installerSourceRoot,
            "kr.fixture.shell.krwidget.zip");

    CreateWidgetArchive(
        shellArchive,
        widgetId:
            "kr.fixture.shell",
        capabilities:
            new[]
            {
                WidgetCapabilityIds.ShellExecute
            });

    await ExpectPackageRejectedAsync(
        () =>
            installer.InstallArchiveAsync(
                shellArchive,
                CancellationToken.None),
        "Prohibited shell capability package rejection failed.");

    var networkArchive =
        Path.Combine(
            installerSourceRoot,
            "kr.fixture.network.krwidget.zip");

    CreateWidgetArchive(
        networkArchive,
        widgetId:
            "kr.fixture.network",
        capabilities:
            new[]
            {
                WidgetCapabilityIds.NetworkHttp
            });

    await ExpectPackageRejectedAsync(
        () =>
            installer.InstallArchiveAsync(
                networkArchive,
                CancellationToken.None),
        "Reserved network capability package rejection failed.");

    Console.WriteLine(
        "Batch 8D1 Widget capability governance smoke test passed.");
}
finally
{
    if (Directory.Exists(
        tempRoot))
    {
        Directory.Delete(
            tempRoot,
            recursive:
                true);
    }
}

static void AssertDecision(
    WidgetCapabilityDecision decision,
    WidgetCapabilityDecisionCode expectedCode,
    string message)
{
    if (decision.Code
        != expectedCode)
    {
        throw new InvalidOperationException(
            message);
    }
}

static async Task ExpectCapabilityDeniedAsync(
    Func<Task> action,
    WidgetCapabilityDecisionCode expectedCode,
    string message)
{
    try
    {
        await action();
    }
    catch (
        WidgetCapabilityDeniedException exception)
    {
        if (
            exception.Decision.Code
            == expectedCode
        )
        {
            return;
        }

        throw new InvalidOperationException(
            message);
    }

    throw new InvalidOperationException(
        message);
}

static async Task ExpectPackageRejectedAsync(
    Func<Task> action,
    string message)
{
    try
    {
        await action();
    }
    catch (
        WidgetPackageValidationException exception)
    {
        if (
            exception.Code
            == WidgetPackageValidationCode.UnsupportedCapability
        )
        {
            return;
        }

        throw new InvalidOperationException(
            message);
    }

    throw new InvalidOperationException(
        message);
}

static void CreateWidgetArchive(
    string archivePath,
    string widgetId,
    string[] capabilities)
{
    using var archive =
        ZipFile.Open(
            archivePath,
            ZipArchiveMode.Create);

    WriteTextEntry(
        archive,
        "manifest.json",
        JsonSerializer.Serialize(
            new WidgetPackageManifest
            {
                ManifestSchemaVersion =
                    1,
                WidgetId =
                    widgetId,
                PackageVersion =
                    "1.0.0",
                MinimumHostVersion =
                    "0.1.0",
                EntryAssembly =
                    "lib/KR.Fixture.Widget.dll",
                EntryType =
                    "KR.Fixture.Widget",
                Capabilities =
                    capabilities
            }));

    WriteBinaryEntry(
        archive,
        "lib/KR.Fixture.Widget.dll",
        new byte[]
        {
            1,
            2,
            3
        });
}

static void WriteTextEntry(
    ZipArchive archive,
    string path,
    string value)
{
    var entry =
        archive.CreateEntry(
            path);

    using var stream =
        entry.Open();

    using var writer =
        new StreamWriter(
            stream,
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier:
                    false));

    writer.Write(
        value);
}

static void WriteBinaryEntry(
    ZipArchive archive,
    string path,
    byte[] value)
{
    var entry =
        archive.CreateEntry(
            path);

    using var stream =
        entry.Open();

    stream.Write(
        value);
}