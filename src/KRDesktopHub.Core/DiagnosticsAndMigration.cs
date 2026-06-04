using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace KRDesktopHub.Core;

public sealed record DiagnosticLogRecord(
    DateTimeOffset TimestampUtc,
    string Level,
    string Category,
    string Message);

public static partial class DiagnosticTextRedactor
{
    [GeneratedRegex(
        @"(?i)\b(token|password|secret|api[_-]?key)\b\s*[:=]\s*[^\s,;]+")]
    private static partial Regex SensitiveAssignmentRegex();

    public static string Redact(
        string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return SensitiveAssignmentRegex()
            .Replace(
                text,
                "$1=[REDACTED]");
    }
}

public sealed class StructuredFileDiagnosticLogger
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = false
        };

    private readonly string _logDirectory;
    private readonly TimeSpan _retention;
    private readonly SemaphoreSlim _gate =
        new(1, 1);

    public StructuredFileDiagnosticLogger(
        string logDirectory,
        TimeSpan retention)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            logDirectory);

        if (retention <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retention));
        }

        _logDirectory =
            logDirectory;

        _retention =
            retention;

        Directory.CreateDirectory(
            _logDirectory);
    }

    public async Task WriteAsync(
        string level,
        string category,
        string message,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            level);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            category);

        ArgumentNullException.ThrowIfNull(
            message);

        var normalizedMessage =
            DiagnosticTextRedactor
                .Redact(message)
                .Replace(
                    Environment.NewLine,
                    " ",
                    StringComparison.Ordinal)
                .Replace(
                    "\n",
                    " ",
                    StringComparison.Ordinal)
                .Replace(
                    "\r",
                    " ",
                    StringComparison.Ordinal);

        if (normalizedMessage.Length > 2048)
        {
            normalizedMessage =
                normalizedMessage[..2048];
        }

        var record =
            new DiagnosticLogRecord(
                DateTimeOffset.UtcNow,
                level,
                category,
                normalizedMessage);

        var path =
            Path.Combine(
                _logDirectory,
                $"corehost-{DateTime.UtcNow:yyyyMMdd}.jsonl");

        var line =
            JsonSerializer.Serialize(
                record,
                JsonOptions);

        await _gate.WaitAsync(
            cancellationToken);

        try
        {
            await File.AppendAllTextAsync(
                path,
                line + Environment.NewLine,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task CleanupExpiredAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var threshold =
            DateTimeOffset.UtcNow - _retention;

        foreach (var file in
            Directory.EnumerateFiles(
                _logDirectory,
                "*.jsonl",
                SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.GetLastWriteTimeUtc(file) <
                threshold.UtcDateTime)
            {
                File.Delete(file);
            }
        }

        return Task.CompletedTask;
    }
}

public sealed class JsonSecretRedactor
{
    private static readonly string[] SensitiveFragments =
    [
        "token",
        "password",
        "secret",
        "api_key",
        "apikey",
        "credential",
        "private_key"
    ];

    public string Redact(
        string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            json);

        var node =
            JsonNode.Parse(json)
            ?? throw new InvalidOperationException(
                "JSON content could not be parsed.");

        RedactNode(
            node);

        return node.ToJsonString(
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
    }

    private static void RedactNode(
        JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in
                jsonObject.ToList())
            {
                if (IsSensitive(
                    property.Key))
                {
                    jsonObject[property.Key] =
                        "[REDACTED]";
                }
                else if (property.Value is not null)
                {
                    RedactNode(
                        property.Value);
                }
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in
                jsonArray)
            {
                if (item is not null)
                {
                    RedactNode(
                        item);
                }
            }
        }
    }

    private static bool IsSensitive(
        string key)
    {
        return SensitiveFragments.Any(
            fragment =>
                key.Contains(
                    fragment,
                    StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record DiagnosticExportOptions(
    bool IncludeSanitizedConfiguration,
    bool IncludeLogFileNames,
    bool IncludeSanitizedLogTails,
    int MaximumLogTailLines)
{
    public static DiagnosticExportOptions Recommended =>
        new(
            IncludeSanitizedConfiguration:
                true,

            IncludeLogFileNames:
                true,

            IncludeSanitizedLogTails:
                false,

            MaximumLogTailLines:
                200);

    public void Validate()
    {
        if (MaximumLogTailLines <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumLogTailLines));
        }
    }
}

public sealed record DiagnosticSnapshot(
    DateTimeOffset ExportedAtUtc,
    string OperatingSystem,
    string Architecture,
    string Framework,
    int ProcessorCount,
    long WorkingSetBytes,
    int ThreadCount);

public sealed class DiagnosticsExporter
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true
        };

    private readonly string _dataRoot;
    private readonly JsonSecretRedactor _jsonRedactor =
        new();

    public DiagnosticsExporter(
        string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataRoot);

        _dataRoot =
            dataRoot;
    }

    public async Task ExportAsync(
        string destinationZip,
        DiagnosticExportOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            destinationZip);

        ArgumentNullException.ThrowIfNull(
            options);

        options.Validate();

        var temporaryRoot =
            Path.Combine(
                Path.GetTempPath(),
                "KRDesktopHub",
                "diagnostics",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(
            temporaryRoot);

        try
        {
            await WriteSnapshotAsync(
                temporaryRoot,
                cancellationToken);

            if (options.IncludeSanitizedConfiguration)
            {
                await CopySanitizedJsonDirectoryAsync(
                    Path.Combine(
                        _dataRoot,
                        "config"),

                    Path.Combine(
                        temporaryRoot,
                        "config-sanitized"),

                    cancellationToken);
            }

            var logDirectory =
                Path.Combine(
                    _dataRoot,
                    "logs");

            if (options.IncludeLogFileNames)
            {
                await WriteLogFileIndexAsync(
                    logDirectory,
                    temporaryRoot,
                    cancellationToken);
            }

            if (options.IncludeSanitizedLogTails)
            {
                await CopySanitizedLogTailsAsync(
                    logDirectory,
                    Path.Combine(
                        temporaryRoot,
                        "log-tails-sanitized"),

                    options.MaximumLogTailLines,
                    cancellationToken);
            }

            CreateZip(
                temporaryRoot,
                destinationZip);
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
    }

    private static async Task WriteSnapshotAsync(
        string temporaryRoot,
        CancellationToken cancellationToken)
    {
        using var process =
            Process.GetCurrentProcess();

        process.Refresh();

        var snapshot =
            new DiagnosticSnapshot(
                DateTimeOffset.UtcNow,
                RuntimeInformation.OSDescription,
                RuntimeInformation.OSArchitecture.ToString(),
                RuntimeInformation.FrameworkDescription,
                Environment.ProcessorCount,
                process.WorkingSet64,
                process.Threads.Count);

        await File.WriteAllTextAsync(
            Path.Combine(
                temporaryRoot,
                "diagnostic-snapshot.json"),

            JsonSerializer.Serialize(
                snapshot,
                JsonOptions),

            cancellationToken);
    }

    private async Task CopySanitizedJsonDirectoryAsync(
        string source,
        string destination,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(
            source))
        {
            return;
        }

        foreach (var file in
            Directory.EnumerateFiles(
                source,
                "*.json",
                SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative =
                Path.GetRelativePath(
                    source,
                    file);

            var target =
                Path.Combine(
                    destination,
                    relative);

            Directory.CreateDirectory(
                Path.GetDirectoryName(target)!);

            var json =
                await File.ReadAllTextAsync(
                    file,
                    cancellationToken);

            await File.WriteAllTextAsync(
                target,
                _jsonRedactor.Redact(json),
                cancellationToken);
        }
    }

    private static async Task WriteLogFileIndexAsync(
        string logDirectory,
        string temporaryRoot,
        CancellationToken cancellationToken)
    {
        var files =
            Directory.Exists(logDirectory)
                ? Directory
                    .EnumerateFiles(
                        logDirectory,
                        "*",
                        SearchOption.TopDirectoryOnly)
                    .Select(
                        file =>
                            Path.GetFileName(file))
                    .OrderBy(
                        file =>
                            file,
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : Array.Empty<string>();

        await File.WriteAllTextAsync(
            Path.Combine(
                temporaryRoot,
                "log-file-index.json"),

            JsonSerializer.Serialize(
                files,
                JsonOptions),

            cancellationToken);
    }

    private static async Task CopySanitizedLogTailsAsync(
        string logDirectory,
        string destination,
        int maximumLines,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(
            logDirectory))
        {
            return;
        }

        Directory.CreateDirectory(
            destination);

        foreach (var file in
            Directory.EnumerateFiles(
                logDirectory,
                "*",
                SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var lines =
                await File.ReadAllLinesAsync(
                    file,
                    cancellationToken);

            var tail =
                lines
                    .TakeLast(
                        maximumLines)
                    .Select(
                        DiagnosticTextRedactor.Redact);

            await File.WriteAllLinesAsync(
                Path.Combine(
                    destination,
                    Path.GetFileName(file)),

                tail,
                cancellationToken);
        }
    }

    private static void CreateZip(
        string sourceDirectory,
        string destinationZip)
    {
        var destinationDirectory =
            Path.GetDirectoryName(
                Path.GetFullPath(
                    destinationZip));

        if (!string.IsNullOrWhiteSpace(
            destinationDirectory))
        {
            Directory.CreateDirectory(
                destinationDirectory);
        }

        if (File.Exists(
            destinationZip))
        {
            File.Delete(
                destinationZip);
        }

        ZipFile.CreateFromDirectory(
            sourceDirectory,
            destinationZip,
            CompressionLevel.SmallestSize,
            includeBaseDirectory:
                false);
    }
}

public sealed record DataMigrationOptions(
    bool IncludeLogs,
    bool IncludeCache)
{
    public static DataMigrationOptions Recommended =>
        new(
            IncludeLogs:
                false,

            IncludeCache:
                false);
}

public sealed record DataMigrationManifest(
    int SchemaVersion,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string> IncludedDirectories);

public sealed record DataMigrationImportResult(
    string BackupZip,
    IReadOnlyList<string> ImportedDirectories);

public sealed class PortableDataMigrationService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true
        };

    private static readonly string[] BaseDirectories =
    [
        "config",
        "state",
        "plugins"
    ];

    public async Task ExportAsync(
        string dataRoot,
        string destinationZip,
        DataMigrationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataRoot);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            destinationZip);

        ArgumentNullException.ThrowIfNull(
            options);

        var directories =
            GetIncludedDirectories(
                options);

        var temporaryRoot =
            Path.Combine(
                Path.GetTempPath(),
                "KRDesktopHub",
                "migration-export",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(
            temporaryRoot);

        try
        {
            var existingDirectories =
                new List<string>();

            foreach (var directory in
                directories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var source =
                    Path.Combine(
                        dataRoot,
                        directory);

                if (!Directory.Exists(
                    source))
                {
                    continue;
                }

                CopyDirectory(
                    source,
                    Path.Combine(
                        temporaryRoot,
                        directory));

                existingDirectories.Add(
                    directory);
            }

            var manifest =
                new DataMigrationManifest(
                    SchemaVersion:
                        1,

                    CreatedAtUtc:
                        DateTimeOffset.UtcNow,

                    IncludedDirectories:
                        existingDirectories);

            await File.WriteAllTextAsync(
                Path.Combine(
                    temporaryRoot,
                    "migration-manifest.json"),

                JsonSerializer.Serialize(
                    manifest,
                    JsonOptions),

                cancellationToken);

            CreateZip(
                temporaryRoot,
                destinationZip);
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
    }

    public async Task<DataMigrationImportResult> ImportAsync(
        string archiveZip,
        string targetDataRoot,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            archiveZip);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            targetDataRoot);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            backupDirectory);

        if (!File.Exists(
            archiveZip))
        {
            throw new FileNotFoundException(
                "Migration archive was not found.",
                archiveZip);
        }

        Directory.CreateDirectory(
            targetDataRoot);

        Directory.CreateDirectory(
            backupDirectory);

        var backupZip =
            Path.Combine(
                backupDirectory,
                $"before-import-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip");

        await ExportAsync(
            targetDataRoot,
            backupZip,
            new DataMigrationOptions(
                IncludeLogs:
                    true,

                IncludeCache:
                    true),

            cancellationToken);

        var stagingRoot =
            Path.Combine(
                Path.GetTempPath(),
                "KRDesktopHub",
                "migration-import",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(
            stagingRoot);

        try
        {
            ExtractSafely(
                archiveZip,
                stagingRoot);

            var manifestPath =
                Path.Combine(
                    stagingRoot,
                    "migration-manifest.json");

            if (!File.Exists(
                manifestPath))
            {
                throw new InvalidDataException(
                    "Migration manifest is missing.");
            }

            var manifest =
                JsonSerializer.Deserialize<DataMigrationManifest>(
                    await File.ReadAllTextAsync(
                        manifestPath,
                        cancellationToken),

                    JsonOptions)
                ?? throw new InvalidDataException(
                    "Migration manifest is invalid.");

            foreach (var directory in
                manifest.IncludedDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!GetAllowedImportDirectories()
                    .Contains(
                        directory,
                        StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Unsupported migration directory: {directory}");
                }

                var source =
                    Path.Combine(
                        stagingRoot,
                        directory);

                if (!Directory.Exists(
                    source))
                {
                    continue;
                }

                CopyDirectory(
                    source,
                    Path.Combine(
                        targetDataRoot,
                        directory));
            }

            return new DataMigrationImportResult(
                backupZip,
                manifest.IncludedDirectories);
        }
        finally
        {
            if (Directory.Exists(
                stagingRoot))
            {
                Directory.Delete(
                    stagingRoot,
                    recursive:
                        true);
            }
        }
    }

    private static IReadOnlyList<string> GetIncludedDirectories(
        DataMigrationOptions options)
    {
        var directories =
            BaseDirectories.ToList();

        if (options.IncludeLogs)
        {
            directories.Add(
                "logs");
        }

        if (options.IncludeCache)
        {
            directories.Add(
                "cache");
        }

        return directories;
    }

    private static IReadOnlyList<string> GetAllowedImportDirectories()
    {
        return
        [
            "config",
            "state",
            "plugins",
            "logs",
            "cache"
        ];
    }

    private static void ExtractSafely(
        string archiveZip,
        string stagingRoot)
    {
        var canonicalRoot =
            Path.GetFullPath(
                stagingRoot)
            + Path.DirectorySeparatorChar;

        using var archive =
            ZipFile.OpenRead(
                archiveZip);

        foreach (var entry in
            archive.Entries)
        {
            var normalizedEntryName =
                entry.FullName.Replace(
                    '/',
                    Path.DirectorySeparatorChar);

            if (string.IsNullOrWhiteSpace(
                normalizedEntryName))
            {
                continue;
            }

            if (Path.IsPathRooted(
                normalizedEntryName))
            {
                throw new InvalidDataException(
                    $"Rooted archive entry is not allowed: {entry.FullName}");
            }

            var destination =
                Path.GetFullPath(
                    Path.Combine(
                        stagingRoot,
                        normalizedEntryName));

            if (!destination.StartsWith(
                canonicalRoot,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Archive entry escapes target directory: {entry.FullName}");
            }

            var firstSegment =
                normalizedEntryName
                    .Split(
                        Path.DirectorySeparatorChar,
                        StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

            if (firstSegment is null)
            {
                continue;
            }

            if (!string.Equals(
                    firstSegment,
                    "migration-manifest.json",
                    StringComparison.OrdinalIgnoreCase)
                && !GetAllowedImportDirectories()
                    .Contains(
                        firstSegment,
                        StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Unsupported archive entry: {entry.FullName}");
            }

            if (entry.FullName.EndsWith(
                "/",
                StringComparison.Ordinal))
            {
                Directory.CreateDirectory(
                    destination);

                continue;
            }

            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    destination)!);

            entry.ExtractToFile(
                destination,
                overwrite:
                    true);
        }
    }

    private static void CopyDirectory(
        string source,
        string destination)
    {
        Directory.CreateDirectory(
            destination);

        foreach (var directory in
            Directory.EnumerateDirectories(
                source,
                "*",
                SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(
                Path.Combine(
                    destination,
                    Path.GetRelativePath(
                        source,
                        directory)));
        }

        foreach (var file in
            Directory.EnumerateFiles(
                source,
                "*",
                SearchOption.AllDirectories))
        {
            var target =
                Path.Combine(
                    destination,
                    Path.GetRelativePath(
                        source,
                        file));

            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    target)!);

            File.Copy(
                file,
                target,
                overwrite:
                    true);
        }
    }

    private static void CreateZip(
        string sourceDirectory,
        string destinationZip)
    {
        var destinationDirectory =
            Path.GetDirectoryName(
                Path.GetFullPath(
                    destinationZip));

        if (!string.IsNullOrWhiteSpace(
            destinationDirectory))
        {
            Directory.CreateDirectory(
                destinationDirectory);
        }

        if (File.Exists(
            destinationZip))
        {
            File.Delete(
                destinationZip);
        }

        ZipFile.CreateFromDirectory(
            sourceDirectory,
            destinationZip,
            CompressionLevel.SmallestSize,
            includeBaseDirectory:
                false);
    }
}