using System.IO.Compression;
using KRDesktopHub.Contracts;
using KRDesktopHub.Core;
using KRDesktopHub.WidgetSdk;

var repositoryRoot =
    Environment.CurrentDirectory;

var temporaryRoot =
    Path.Combine(
        Path.GetTempPath(),
        "KRDesktopHub",
        "batch6-smoke",
        Guid.NewGuid().ToString("N"));

Directory.CreateDirectory(
    temporaryRoot);

try
{
    var sourceDataRoot =
        Path.Combine(
            temporaryRoot,
            "source-data");

    var targetDataRoot =
        Path.Combine(
            temporaryRoot,
            "target-data");

    var backupRoot =
        Path.Combine(
            temporaryRoot,
            "backups");

    var archiveRoot =
        Path.Combine(
            temporaryRoot,
            "archives");

    Directory.CreateDirectory(
        Path.Combine(
            sourceDataRoot,
            "config"));

    Directory.CreateDirectory(
        Path.Combine(
            sourceDataRoot,
            "state"));

    Directory.CreateDirectory(
        Path.Combine(
            sourceDataRoot,
            "plugins"));

    Directory.CreateDirectory(
        Path.Combine(
            sourceDataRoot,
            "logs"));

    Directory.CreateDirectory(
        archiveRoot);

    await File.WriteAllTextAsync(
        Path.Combine(
            sourceDataRoot,
            "config",
            "host.json"),

        """
        {
          "visible_setting": "keep-me",
          "api_key": "do-not-export",
          "nested": {
            "password": "do-not-export"
          }
        }
        """);

    await File.WriteAllTextAsync(
        Path.Combine(
            sourceDataRoot,
            "state",
            "widget-state.json"),

        """
        {
          "counter": 42
        }
        """);

    await File.WriteAllTextAsync(
        Path.Combine(
            sourceDataRoot,
            "plugins",
            "plugin.txt"),

        "plugin-data");

    var logger =
        new StructuredFileDiagnosticLogger(
            Path.Combine(
                sourceDataRoot,
                "logs"),

            TimeSpan.FromDays(14));

    await logger.WriteAsync(
        "INFO",
        "smoke",
        "token=do-not-log visible-message",
        CancellationToken.None);

    await logger.CleanupExpiredAsync(
        CancellationToken.None);

    var diagnosticsZip =
        Path.Combine(
            archiveRoot,
            "diagnostics.zip");

    await new DiagnosticsExporter(
        sourceDataRoot)
        .ExportAsync(
            diagnosticsZip,
            DiagnosticExportOptions.Recommended,
            CancellationToken.None);

    using (var diagnosticsArchive =
        ZipFile.OpenRead(
            diagnosticsZip))
    {
        var names =
            diagnosticsArchive
                .Entries
                .Select(
                    entry =>
                        entry.FullName)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        if (!names.Contains(
            "diagnostic-snapshot.json"))
        {
            throw new InvalidOperationException(
                "Diagnostic snapshot is missing.");
        }

        if (!names.Contains(
            "config-sanitized/host.json"))
        {
            throw new InvalidOperationException(
                "Sanitized configuration is missing.");
        }

        if (!names.Contains(
            "log-file-index.json"))
        {
            throw new InvalidOperationException(
                "Log-file index is missing.");
        }

        if (names.Any(
            name =>
                name.StartsWith(
                    "log-tails-sanitized/",
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Log tails must be excluded by default.");
        }

        var configEntry =
            diagnosticsArchive
                .GetEntry(
                    "config-sanitized/host.json")
            ?? throw new InvalidOperationException(
                "Sanitized configuration entry is missing.");

        using var reader =
            new StreamReader(
                configEntry.Open());

        var sanitizedJson =
            await reader.ReadToEndAsync();

        if (sanitizedJson.Contains(
            "do-not-export",
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Sensitive JSON field was not redacted.");
        }

        if (!sanitizedJson.Contains(
            "[REDACTED]",
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Expected JSON redaction marker is missing.");
        }
    }

    var migration =
        new PortableDataMigrationService();

    var migrationZip =
        Path.Combine(
            archiveRoot,
            "migration.zip");

    await migration.ExportAsync(
        sourceDataRoot,
        migrationZip,
        DataMigrationOptions.Recommended,
        CancellationToken.None);

    Directory.CreateDirectory(
        Path.Combine(
            targetDataRoot,
            "config"));

    await File.WriteAllTextAsync(
        Path.Combine(
            targetDataRoot,
            "config",
            "before-import.json"),

        """
        {
          "existing": true
        }
        """);

    var importResult =
        await migration.ImportAsync(
            migrationZip,
            targetDataRoot,
            backupRoot,
            CancellationToken.None);

    if (!File.Exists(
        importResult.BackupZip))
    {
        throw new InvalidOperationException(
            "Pre-import backup archive is missing.");
    }

    if (!File.Exists(
        Path.Combine(
            targetDataRoot,
            "state",
            "widget-state.json")))
    {
        throw new InvalidOperationException(
            "Imported state file is missing.");
    }

    if (Directory.Exists(
        Path.Combine(
            targetDataRoot,
            "logs")))
    {
        throw new InvalidOperationException(
            "Logs must remain excluded from the recommended migration export.");
    }

    var maliciousZip =
        Path.Combine(
            archiveRoot,
            "malicious.zip");

    using (var maliciousArchive =
        ZipFile.Open(
            maliciousZip,
            ZipArchiveMode.Create))
    {
        var manifestEntry =
            maliciousArchive.CreateEntry(
                "migration-manifest.json");

        await using (var writer =
            new StreamWriter(
                manifestEntry.Open()))
        {
            await writer.WriteAsync(
                """
                {
                  "schemaVersion": 1,
                  "createdAtUtc": "2026-01-01T00:00:00+00:00",
                  "includedDirectories": []
                }
                """);
        }

        var escapeEntry =
            maliciousArchive.CreateEntry(
                "../escape.txt");

        await using var escapeWriter =
            new StreamWriter(
                escapeEntry.Open());

        await escapeWriter.WriteAsync(
            "must-not-escape");
    }

    var blockedEscape =
        false;

    try
    {
        await migration.ImportAsync(
            maliciousZip,
            Path.Combine(
                temporaryRoot,
                "malicious-target"),

            Path.Combine(
                temporaryRoot,
                "malicious-backups"),

            CancellationToken.None);
    }
    catch (InvalidDataException)
    {
        blockedEscape =
            true;
    }

    if (!blockedEscape)
    {
        throw new InvalidOperationException(
            "Archive path traversal was not blocked.");
    }

    if (File.Exists(
        Path.Combine(
            temporaryRoot,
            "escape.txt")))
    {
        throw new InvalidOperationException(
            "Archive entry escaped the target directory.");
    }

    var localManifestPath =
        Path.Combine(
            temporaryRoot,
            "sdk-manifest.json");

    await WidgetManifestFile.WriteAsync(
        localManifestPath,
        WidgetManifestFile.Create(
            new SdkFixtureWidget(),
            "Fixture.dll",
            "Fixture.Widget",
            WidgetActivationMode.OnDemand),
        CancellationToken.None);

    var manifestText =
        await File.ReadAllTextAsync(
            localManifestPath);

    if (!manifestText.Contains(
        "\"widgetId\": \"kr.fixture.sdk\"",
        StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "Widget SDK manifest generation failed.");
    }

    var services =
        CoreRuntimeFactory.Create(
            Path.Combine(
                temporaryRoot,
                "hello-runtime"),

            Path.Combine(
                repositoryRoot,
                "resources"),

            Path.Combine(
                temporaryRoot,
                "hello-settings.json"));

    await using var scheduler =
        new PeriodicWidgetScheduler();

    var context =
        new DefaultWidgetContext(
            services.Logger,
            scheduler,
            services.StateStore,
            services.SettingsStore,
            services.EventBus,
            services.Commands,
            services.Clock,
            services.Localization,
            new NullWidgetNotificationClient());

    var loader =
        new WidgetPluginLoader(
            new Version(0, 1, 0));

    using var loaded =
        await loader.LoadAsync(
            Path.Combine(
                repositoryRoot,
                "samples",
                "HelloWidget"),

            CancellationToken.None);

    await loaded.Widget.InitializeAsync(
        context,
        CancellationToken.None);

    await loaded.Widget.StartAsync(
        CancellationToken.None);

    var startCount =
        await services
            .StateStore
            .ReadAsync<int>(
                "hello.start_count",
                CancellationToken.None);

    if (startCount != 1)
    {
        throw new InvalidOperationException(
            "HelloWidget dynamic-load validation failed.");
    }

    Console.WriteLine(
        "Batch 6 Diagnostics Migration and Widget SDK smoke test passed.");
}
finally
{
    if (Directory.Exists(
        temporaryRoot))
    {
        Directory.Delete(
            temporaryRoot,
            recursive:
                true);
    }
}

public sealed class SdkFixtureWidget
    : KrWidgetBase
{
    public override WidgetDescriptor Descriptor { get; } =
        new(
            "kr.fixture.sdk",
            "SDK Fixture Widget",
            new Version(0, 1, 0),
            new Version(1, 0, 0),
            new Version(0, 1, 0),
            new[]
            {
                "sdk"
            });
}