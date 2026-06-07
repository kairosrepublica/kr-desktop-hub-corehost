using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KRDesktopHub.Core;

var tempRoot =
    Path.Combine(
        Path.GetTempPath(),
        "KRDesktopHub-WidgetManager-"
        + Guid
            .NewGuid()
            .ToString(
                "N"));

Directory.CreateDirectory(
    tempRoot);

try
{
    var dataRoot =
        Path.Combine(
            tempRoot,
            "data");

    var sourceRoot =
        Path.Combine(
            tempRoot,
            "source");

    Directory.CreateDirectory(
        sourceRoot);

    var recommendedOptions =
        WidgetPackageInstallerOptions.CreateRecommended(
            dataRoot,
            new Version(
                0,
                1,
                0));

    var manager =
        new InternalWidgetManagerService(
            new InternalWidgetPackageInstaller(
                recommendedOptions));

    var markerPath =
        Path.Combine(
            tempRoot,
            "MUST_NOT_EXIST.txt");

    var inboxArchive =
        Path.Combine(
            manager.InboxDirectory,
            "kr.fixture.manager.inbox.krwidget.zip");

    CreateWidgetArchive(
        inboxArchive,
        widgetId:
            "kr.fixture.manager.inbox",
        packageVersion:
            "1.0.0",
        new Dictionary<string, string>
        {
            ["scripts/do_not_run.ps1"] =
                $"Set-Content -LiteralPath '{markerPath}' -Value 'unexpected execution'"
        });

    var discovered =
        manager.RefreshInbox();

    if (
        discovered.Count
        != 1
        || !string.Equals(
            discovered[0].FullPath,
            inboxArchive,
            StringComparison.OrdinalIgnoreCase)
    )
    {
        throw new InvalidOperationException(
            "Widget Manager inbox refresh validation failed.");
    }

    if (Directory.Exists(
        Path.Combine(
            dataRoot,
            "plugins",
            "installed",
            "kr.fixture.manager.inbox")))
    {
        throw new InvalidOperationException(
            "Widget Manager refresh unexpectedly installed an inbox package.");
    }

    if (File.Exists(
        markerPath))
    {
        throw new InvalidOperationException(
            "Widget Manager refresh unexpectedly ran a dropped script.");
    }

    var inboxInstall =
        await manager.InstallInboxArchiveAsync(
            inboxArchive,
            CancellationToken.None);

    if (
        inboxInstall.WidgetId
        != "kr.fixture.manager.inbox"
        || !Directory.Exists(
            inboxInstall.InstalledPath)
    )
    {
        throw new InvalidOperationException(
            "Explicit inbox installation validation failed.");
    }

    if (File.Exists(
        markerPath))
    {
        throw new InvalidOperationException(
            "Explicit package installation unexpectedly ran an embedded script.");
    }

    var stagedCandidate =
        await manager
            .DiscoverInstalledWidgetsAsync(
                CancellationToken.None);

    if (
        stagedCandidate.Widgets.Count != 1
        || manager
            .GetInstalledWidgetLayout()
            .Widgets
            .Count
            != 0
    )
    {
        throw new InvalidOperationException(
            "Pure installed-catalog staging unexpectedly mutated active layout registration.");
    }

    var installedInventory =
        manager
            .CommitAcceptedInstalledWidgets(
                stagedCandidate);

    var installedInboxWidget =
        installedInventory
            .Widgets
            .Single(
                widget =>
                    widget.WidgetId
                    == "kr.fixture.manager.inbox");

    if (
        !installedInboxWidget.Enabled
        || installedInboxWidget.Collapsed
        || installedInboxWidget.PackageVersion
            != new Version(
                1,
                0,
                0)
        || installedInboxWidget.ActualHeightDip
            != 220
    )
    {
        throw new InvalidOperationException(
            "Installed Widget inventory validation failed.");
    }

    var disabledLayout =
        manager.SetInstalledWidgetEnabled(
            "kr.fixture.manager.inbox",
            enabled:
                false);

    if (disabledLayout
        .Widgets
        .Single(
            widget =>
                widget.WidgetId
                == "kr.fixture.manager.inbox")
        .Enabled)
    {
        throw new InvalidOperationException(
            "Installed Widget disable validation failed.");
    }

    manager.SetInstalledWidgetEnabled(
        "kr.fixture.manager.inbox",
        enabled:
            true);

    var collapsedLayout =
        manager.SetInstalledWidgetCollapsed(
            "kr.fixture.manager.inbox",
            collapsed:
                true);

    if (!collapsedLayout
        .Widgets
        .Single(
            widget =>
                widget.WidgetId
                == "kr.fixture.manager.inbox")
        .Collapsed)
    {
        throw new InvalidOperationException(
            "Installed Widget collapse validation failed.");
    }

    var orderedLayout =
        manager.SetInstalledWidgetOrder(
            "kr.fixture.manager.inbox",
            order:
                90);

    if (orderedLayout
        .Widgets
        .Single(
            widget =>
                widget.WidgetId
                == "kr.fixture.manager.inbox")
        .Order
        != 90)
    {
        throw new InvalidOperationException(
            "Installed Widget order validation failed.");
    }

    var transactionalRoot =
        Path.Combine(
            tempRoot,
            "transactional-catalog");

    var transactionalInstalledRoot =
        Path.Combine(
            transactionalRoot,
            "installed");

    var transactionalStatePath =
        Path.Combine(
            transactionalRoot,
            "state",
            "widget-host-state.json");

    var transactionalWidgetPath =
        Path.Combine(
            transactionalInstalledRoot,
            "kr.fixture.manager.transactional");

    CreateDevelopmentFolder(
        transactionalWidgetPath,
        widgetId:
            "kr.fixture.manager.transactional");

    var transactionalController =
        new WidgetHostLayoutController(
            new JsonWidgetHostStateStore(
                transactionalStatePath));

    var transactionalCatalog =
        new InstalledWidgetCatalogService(
            transactionalInstalledRoot,
            transactionalController);

    var transactionalStaged =
        await transactionalCatalog
            .DiscoverAsync(
                CancellationToken.None);

    if (
        transactionalStaged.Widgets.Count != 1
        || transactionalController
            .GetLayout()
            .Widgets
            .Count
            != 0
        || File.Exists(
            transactionalStatePath)
    )
    {
        throw new InvalidOperationException(
            "Installed-catalog staging unexpectedly mutated or persisted host state.");
    }

    var transactionalCommitted =
        transactionalCatalog
            .CommitAcceptedCandidate(
                transactionalStaged);

    if (
        transactionalCommitted.Widgets.Count != 1
        || !File.Exists(
            transactionalStatePath)
    )
    {
        throw new InvalidOperationException(
            "Accepted installed-catalog candidate did not commit host state.");
    }

    _ =
        transactionalController
            .SetCollapsed(
                "kr.fixture.manager.transactional",
                collapsed:
                    true);

    _ =
        transactionalController
            .UpdateMeasuredHeight(
                "kr.fixture.manager.transactional",
                measuredDesiredHeightDip:
                    999);

    var committedStateHash =
        ComputeSha256(
            transactionalStatePath);

    File.Delete(
        Path.Combine(
            transactionalWidgetPath,
            "manifest.json"));

    var rejectedCandidate =
        await transactionalCatalog
            .DiscoverAsync(
                CancellationToken.None);

    if (WidgetHostCatalogRefreshAcceptancePolicy
        .ShouldApply(
            transactionalCommitted,
            rejectedCandidate))
    {
        throw new InvalidOperationException(
            "Degraded transactional candidate unexpectedly passed acceptance.");
    }

    if (
        ComputeSha256(
            transactionalStatePath)
            != committedStateHash
        || transactionalController
            .GetLayout()
            .Widgets
            .Count
            != 1
    )
    {
        throw new InvalidOperationException(
            "Rejected transactional candidate mutated accepted host state.");
    }

    Directory.Delete(
        transactionalWidgetPath,
        recursive:
            true);

    var acceptedRemovalCandidate =
        await transactionalCatalog
            .DiscoverAsync(
                CancellationToken.None);

    if (!WidgetHostCatalogRefreshAcceptancePolicy
        .ShouldApply(
            transactionalCommitted,
            acceptedRemovalCandidate))
    {
        throw new InvalidOperationException(
            "Failure-free explicit removal candidate was incorrectly rejected.");
    }

    var removedTransactionalSnapshot =
        transactionalCatalog
            .CommitAcceptedCandidate(
                acceptedRemovalCandidate);

    if (
        removedTransactionalSnapshot.Widgets.Count != 0
        || transactionalController
            .GetLayout()
            .Widgets
            .Count
            != 0
    )
    {
        throw new InvalidOperationException(
            "Accepted removal did not prune active transactional registration.");
    }

    CreateDevelopmentFolder(
        transactionalWidgetPath,
        widgetId:
            "kr.fixture.manager.transactional");

    var reinstalledTransactionalSnapshot =
        transactionalCatalog
            .CommitAcceptedCandidate(
                await transactionalCatalog
                    .DiscoverAsync(
                        CancellationToken.None));

    var reinstalledTransactionalWidget =
        reinstalledTransactionalSnapshot
            .Widgets
            .Single(
                widget =>
                    widget.WidgetId
                    == "kr.fixture.manager.transactional");

    if (
        !reinstalledTransactionalWidget.Collapsed
        || reinstalledTransactionalWidget.MeasuredDesiredHeightDip
            != reinstalledTransactionalWidget.PreferredExpandedHeightDip
    )
    {
        throw new InvalidOperationException(
            "Dormant Owner preference or stale measured-height pruning validation failed.");
    }

    var outsideArchive =
        Path.Combine(
            sourceRoot,
            "kr.fixture.manager.filepicker.krwidget.zip");

    CreateWidgetArchive(
        outsideArchive,
        widgetId:
            "kr.fixture.manager.filepicker",
        packageVersion:
            "1.0.0",
        extraEntries:
            null);

    var selectedInstall =
        await manager.InstallSelectedArchiveAsync(
            outsideArchive,
            CancellationToken.None);

    if (
        selectedInstall.WidgetId
        != "kr.fixture.manager.filepicker"
        || !Directory.Exists(
            selectedInstall.InstalledPath)
    )
    {
        throw new InvalidOperationException(
            "Explicit file-picker installation validation failed.");
    }

    await ExpectFailureAsync(
        () =>
            manager.InstallInboxArchiveAsync(
                outsideArchive,
                CancellationToken.None),
        "Outside-inbox installation route unexpectedly accepted a file-picker archive.");

    var developmentFolder =
        Path.Combine(
            sourceRoot,
            "development-folder");

    CreateDevelopmentFolder(
        developmentFolder,
        widgetId:
            "kr.fixture.manager.development");

    await ExpectValidationFailureAsync(
        () =>
            manager.InstallDevelopmentFolderAsync(
                developmentFolder,
                CancellationToken.None),
        WidgetPackageValidationCode.DevelopmentFolderInstallDisabled,
        "Default Widget Manager unexpectedly allowed development-folder installation.");

    var advancedManager =
        new InternalWidgetManagerService(
            new InternalWidgetPackageInstaller(
                recommendedOptions with
                {
                    AllowDevelopmentFolderInstall =
                        true
                }));

    var developmentInstall =
        await advancedManager.InstallDevelopmentFolderAsync(
            developmentFolder,
            CancellationToken.None);

    if (
        developmentInstall.WidgetId
        != "kr.fixture.manager.development"
        || developmentInstall.SourceMode
        != WidgetPackageSourceMode.DevelopmentFolder
        || !Directory.Exists(
            developmentInstall.InstalledPath)
    )
    {
        throw new InvalidOperationException(
            "Advanced explicit development-folder installation validation failed.");
    }

    Console.WriteLine(
        "Batch 8C2 Widget Manager smoke test passed.");
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

static string ComputeSha256(
    string path)
{
    return Convert.ToHexString(
        SHA256.HashData(
            File.ReadAllBytes(
                path)));
}

static void CreateWidgetArchive(
    string archivePath,
    string widgetId,
    string packageVersion,
    IReadOnlyDictionary<string, string>? extraEntries)
{
    Directory.CreateDirectory(
        Path.GetDirectoryName(
            archivePath)!);

    using var archive =
        ZipFile.Open(
            archivePath,
            ZipArchiveMode.Create);

    WriteTextEntry(
        archive,
        "manifest.json",
        JsonSerializer.Serialize(
            CreateManifest(
                widgetId,
                packageVersion)));

    WriteBinaryEntry(
        archive,
        "lib/KR.Fixture.Widget.dll",
        new byte[]
        {
            1,
            2,
            3
        });

    if (extraEntries is null)
    {
        return;
    }

    foreach (var pair in
        extraEntries)
    {
        WriteTextEntry(
            archive,
            pair.Key,
            pair.Value);
    }
}

static void CreateDevelopmentFolder(
    string folderPath,
    string widgetId)
{
    Directory.CreateDirectory(
        Path.Combine(
            folderPath,
            "lib"));

    File.WriteAllText(
        Path.Combine(
            folderPath,
            "manifest.json"),
        JsonSerializer.Serialize(
            CreateManifest(
                widgetId,
                "1.0.0")));

    File.WriteAllBytes(
        Path.Combine(
            folderPath,
            "lib",
            "KR.Fixture.Widget.dll"),
        new byte[]
        {
            1,
            2,
            3
        });
}

static WidgetPackageManifest CreateManifest(
    string widgetId,
    string packageVersion)
{
    return new WidgetPackageManifest
    {
        ManifestSchemaVersion =
            1,
        WidgetId =
            widgetId,
        PackageVersion =
            packageVersion,
        MinimumHostVersion =
            "0.1.0",
        EntryAssembly =
            "lib/KR.Fixture.Widget.dll",
        EntryType =
            "KR.Fixture.Widget",
        Capabilities =
            Array.Empty<string>()
    };
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

static async Task ExpectFailureAsync(
    Func<Task> action,
    string message)
{
    try
    {
        await action();
    }
    catch (InvalidOperationException)
    {
        return;
    }

    throw new InvalidOperationException(
        message);
}

static async Task ExpectValidationFailureAsync(
    Func<Task> action,
    WidgetPackageValidationCode expectedCode,
    string message)
{
    try
    {
        await action();

        throw new InvalidOperationException(
            message);
    }
    catch (
        WidgetPackageValidationException exception)
    {
        if (
            exception.Code
            != expectedCode
        )
        {
            throw new InvalidOperationException(
                message);
        }
    }
}
