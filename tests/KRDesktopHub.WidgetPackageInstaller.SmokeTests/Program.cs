using System.IO.Compression;
using System.Text;
using System.Text.Json;
using KRDesktopHub.Core;

var tempRoot =
    Path.Combine(
        Path.GetTempPath(),
        "KRDesktopHub-WidgetPackageInstaller-"
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

    var options =
        WidgetPackageInstallerOptions.CreateRecommended(
            dataRoot,
            new Version(
                0,
                1,
                0),
            new[]
            {
                "clock.read",
                "network.http"
            });

    var installer =
        new InternalWidgetPackageInstaller(
            options);

    var discoveredArchive =
        Path.Combine(
            installer.InboxDirectory,
            "kr.fixture.discovery.krwidget.zip");

    CreateWidgetArchive(
        discoveredArchive,
        CreateManifest(
            widgetId:
                "kr.fixture.discovery",
            packageVersion:
                "1.0.0",
            capabilities:
                new[]
                {
                    "clock.read"
                }),
        extraEntries:
            null);

    File.WriteAllText(
        Path.Combine(
            installer.InboxDirectory,
            "ignore.zip"),
        "not a Widget package");

    File.WriteAllText(
        Path.Combine(
            installer.InboxDirectory,
            "README.txt"),
        "discovery must not execute arbitrary dropped files");

    var discovered =
        installer.DiscoverInboxArchives();

    if (
        discovered.Count
        != 1
        || !string.Equals(
            discovered[0],
            discoveredArchive,
            StringComparison.OrdinalIgnoreCase)
    )
    {
        throw new InvalidOperationException(
            "Inbox discovery validation failed.");
    }

    if (Directory.Exists(
        Path.Combine(
            installer.InstalledDirectory,
            "kr.fixture.discovery")))
    {
        throw new InvalidOperationException(
            "Inbox discovery unexpectedly installed a dropped file.");
    }

    var doNotRunMarker =
        Path.Combine(
            tempRoot,
            "DO_NOT_RUN_MARKER.txt");

    var firstArchive =
        Path.Combine(
            sourceRoot,
            "kr.fixture.package.krwidget.zip");

    CreateWidgetArchive(
        firstArchive,
        CreateManifest(
            widgetId:
                "kr.fixture.package",
            packageVersion:
                "1.0.0",
            capabilities:
                new[]
                {
                    "clock.read"
                }),
        new Dictionary<string, string>
        {
            ["version.txt"] =
                "1.0.0",
            ["scripts/do_not_run.ps1"] =
                $"Set-Content -LiteralPath '{doNotRunMarker}' -Value 'unexpected execution'"
        });

    var firstInstall =
        await installer.InstallArchiveAsync(
            firstArchive,
            CancellationToken.None);

    if (
        firstInstall.WidgetId
        != "kr.fixture.package"
        || firstInstall.PackageVersion
        != new Version(
            1,
            0,
            0)
        || firstInstall.SourceMode
        != WidgetPackageSourceMode.Archive
        || firstInstall.BackupPath is not null
        || !Directory.Exists(
            firstInstall.InstalledPath)
    )
    {
        throw new InvalidOperationException(
            "Initial archive installation validation failed.");
    }

    if (File.Exists(
        doNotRunMarker))
    {
        throw new InvalidOperationException(
            "Installer executed an arbitrary dropped script.");
    }

    var secondArchive =
        Path.Combine(
            sourceRoot,
            "kr.fixture.package.update.krwidget.zip");

    CreateWidgetArchive(
        secondArchive,
        CreateManifest(
            widgetId:
                "kr.fixture.package",
            packageVersion:
                "1.1.0",
            capabilities:
                new[]
                {
                    "clock.read"
                }),
        new Dictionary<string, string>
        {
            ["version.txt"] =
                "1.1.0"
        });

    var secondInstall =
        await installer.InstallArchiveAsync(
            secondArchive,
            CancellationToken.None);

    if (
        secondInstall.PackageVersion
        != new Version(
            1,
            1,
            0)
        || secondInstall.BackupPath is null
        || !Directory.Exists(
            secondInstall.BackupPath)
        || File.ReadAllText(
            Path.Combine(
                secondInstall.InstalledPath,
                "version.txt"))
            != "1.1.0"
    )
    {
        throw new InvalidOperationException(
            "Atomic replacement and backup validation failed.");
    }

    var forbiddenArchive =
        Path.Combine(
            sourceRoot,
            "kr.fixture.forbidden.krwidget.zip");

    CreateWidgetArchive(
        forbiddenArchive,
        CreateManifest(
            widgetId:
                "kr.fixture.forbidden",
            packageVersion:
                "1.0.0",
            capabilities:
                new[]
                {
                    "shell.execute"
                }),
        extraEntries:
            null);

    await ExpectValidationFailureAsync(
        () =>
            installer.InstallArchiveAsync(
                forbiddenArchive,
                CancellationToken.None),
        WidgetPackageValidationCode.UnsupportedCapability,
        "Unsupported capability validation failed.");

    if (!Directory
        .EnumerateFiles(
            installer.QuarantineDirectory,
            "*.reason.txt",
            SearchOption.TopDirectoryOnly)
        .Any())
    {
        throw new InvalidOperationException(
            "Rejected-package quarantine validation failed.");
    }

    var traversalArchive =
        Path.Combine(
            sourceRoot,
            "kr.fixture.traversal.krwidget.zip");

    CreateTraversalArchive(
        traversalArchive);

    var escapedPath =
        Path.Combine(
            installer.StagingDirectory,
            "escape.txt");

    await ExpectValidationFailureAsync(
        () =>
            installer.InstallArchiveAsync(
                traversalArchive,
                CancellationToken.None),
        WidgetPackageValidationCode.UnsafeArchiveEntry,
        "Archive traversal validation failed.");

    if (File.Exists(
        escapedPath))
    {
        throw new InvalidOperationException(
            "Archive traversal escaped the staging directory.");
    }

    var developmentFolder =
        Path.Combine(
            sourceRoot,
            "development-folder");

    Directory.CreateDirectory(
        Path.Combine(
            developmentFolder,
            "lib"));

    File.WriteAllText(
        Path.Combine(
            developmentFolder,
            "manifest.json"),
        JsonSerializer.Serialize(
            CreateManifest(
                widgetId:
                    "kr.fixture.development",
                packageVersion:
                    "1.0.0",
                capabilities:
                    new[]
                    {
                        "clock.read"
                    })));

    File.WriteAllBytes(
        Path.Combine(
            developmentFolder,
            "lib",
            "KR.Fixture.Widget.dll"),
        new byte[]
        {
            1,
            2,
            3
        });

    await ExpectValidationFailureAsync(
        () =>
            installer.InstallDevelopmentFolderAsync(
                developmentFolder,
                CancellationToken.None),
        WidgetPackageValidationCode.DevelopmentFolderInstallDisabled,
        "Development-folder default-deny validation failed.");

    Console.WriteLine(
        "Batch 8C1 Widget package installer smoke test passed.");
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

static WidgetPackageManifest CreateManifest(
    string widgetId,
    string packageVersion,
    string[] capabilities)
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
            capabilities
    };
}

static void CreateWidgetArchive(
    string archivePath,
    WidgetPackageManifest manifest,
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
            manifest));

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

static void CreateTraversalArchive(
    string archivePath)
{
    using var archive =
        ZipFile.Open(
            archivePath,
            ZipArchiveMode.Create);

    WriteTextEntry(
        archive,
        "../escape.txt",
        "must not escape staging");
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