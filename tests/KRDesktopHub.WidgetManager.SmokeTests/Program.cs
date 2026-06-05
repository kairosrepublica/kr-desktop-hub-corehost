using System.IO.Compression;
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