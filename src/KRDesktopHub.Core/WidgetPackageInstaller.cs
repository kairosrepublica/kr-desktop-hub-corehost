using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace KRDesktopHub.Core;

public enum WidgetPackageSourceMode
{
    Archive,
    DevelopmentFolder
}

public enum WidgetPackageValidationCode
{
    InvalidArchiveExtension,
    ArchiveEntryLimitExceeded,
    ArchiveSizeLimitExceeded,
    UnsafeArchiveEntry,
    DuplicateArchiveEntry,
    MissingManifest,
    InvalidManifest,
    UnsupportedManifestSchema,
    InvalidWidgetId,
    InvalidPackageVersion,
    InvalidMinimumHostVersion,
    HostVersionIncompatible,
    InvalidEntryAssembly,
    MissingEntryAssembly,
    MissingEntryType,
    UnsupportedCapability,
    DevelopmentFolderInstallDisabled,
    UnsafeDevelopmentFolderEntry
}

public sealed class WidgetPackageValidationException
    : InvalidOperationException
{
    public WidgetPackageValidationException(
        WidgetPackageValidationCode code,
        string message)
        : base(
            message)
    {
        Code =
            code;
    }

    public WidgetPackageValidationCode Code { get; }
}

public sealed class WidgetPackageManifest
{
    [JsonPropertyName(
        "manifest_schema_version")]
    public int ManifestSchemaVersion { get; set; }

    [JsonPropertyName(
        "widget_id")]
    public string WidgetId { get; set; } =
        string.Empty;

    [JsonPropertyName(
        "package_version")]
    public string PackageVersion { get; set; } =
        string.Empty;

    [JsonPropertyName(
        "minimum_host_version")]
    public string MinimumHostVersion { get; set; } =
        string.Empty;

    [JsonPropertyName(
        "entry_assembly")]
    public string EntryAssembly { get; set; } =
        string.Empty;

    [JsonPropertyName(
        "entry_type")]
    public string EntryType { get; set; } =
        string.Empty;

    [JsonPropertyName(
        "capabilities")]
    public string[] Capabilities { get; set; } =
        Array.Empty<string>();
}

public sealed record WidgetPackageInstallerOptions(
    string DataRoot,
    Version HostVersion,
    IReadOnlySet<string> AllowedCapabilities,
    bool AllowDevelopmentFolderInstall,
    int MaximumArchiveEntries,
    long MaximumExpandedBytes)
{
    public static WidgetPackageInstallerOptions CreateRecommended(
        string dataRoot,
        Version hostVersion,
        IEnumerable<string>? allowedCapabilities =
            null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataRoot);

        ArgumentNullException.ThrowIfNull(
            hostVersion);

        return new WidgetPackageInstallerOptions(
            DataRoot:
                Path.GetFullPath(
                    dataRoot),
            HostVersion:
                hostVersion,
            AllowedCapabilities:
                new HashSet<string>(
                    allowedCapabilities
                        ?? Array.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase),
            AllowDevelopmentFolderInstall:
                false,
            MaximumArchiveEntries:
                512,
            MaximumExpandedBytes:
                128L
                * 1024L
                * 1024L);
    }
}

public sealed record WidgetPackageInstallResult(
    string WidgetId,
    Version PackageVersion,
    WidgetPackageSourceMode SourceMode,
    string InstalledPath,
    string? BackupPath);

public sealed class InternalWidgetPackageInstaller
{
    public const string ProductionArchiveExtension =
        ".krwidget.zip";

    public const string ManifestFileName =
        "manifest.json";

    private static readonly Regex WidgetIdRegex =
        new(
            "^[a-z0-9]+(?:[._-][a-z0-9]+)*$",
            RegexOptions.CultureInvariant
            | RegexOptions.Compiled);

    private readonly WidgetPackageInstallerOptions _options;

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            PropertyNameCaseInsensitive =
                false,
            ReadCommentHandling =
                JsonCommentHandling.Disallow,
            AllowTrailingCommas =
                false
        };

    public InternalWidgetPackageInstaller(
        WidgetPackageInstallerOptions options)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        if (
            options.MaximumArchiveEntries
            < 1
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Maximum archive entries must be positive.");
        }

        if (
            options.MaximumExpandedBytes
            < 1
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Maximum expanded bytes must be positive.");
        }

        _options =
            options;

        EnsureDirectory(
            InboxDirectory);

        EnsureDirectory(
            InstalledDirectory);

        EnsureDirectory(
            StagingDirectory);

        EnsureDirectory(
            BackupDirectory);

        EnsureDirectory(
            QuarantineDirectory);
    }

    public string PluginsDirectory =>
        Path.Combine(
            _options.DataRoot,
            "plugins");

    public string InboxDirectory =>
        Path.Combine(
            PluginsDirectory,
            "inbox");

    public string InstalledDirectory =>
        Path.Combine(
            PluginsDirectory,
            "installed");

    public string StagingDirectory =>
        Path.Combine(
            PluginsDirectory,
            "staging");

    public string BackupDirectory =>
        Path.Combine(
            PluginsDirectory,
            "backups");

    public string QuarantineDirectory =>
        Path.Combine(
            PluginsDirectory,
            "quarantine");

    public IReadOnlyList<string> DiscoverInboxArchives()
    {
        EnsureDirectory(
            InboxDirectory);

        return Directory
            .EnumerateFiles(
                InboxDirectory,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(
                HasProductionArchiveExtension)
            .OrderBy(
                path =>
                    path,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<WidgetPackageInstallResult> InstallArchiveAsync(
        string archivePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            archivePath);

        cancellationToken.ThrowIfCancellationRequested();

        var fullArchivePath =
            Path.GetFullPath(
                archivePath);

        if (!File.Exists(
            fullArchivePath))
        {
            throw new FileNotFoundException(
                "Widget package archive was not found.",
                fullArchivePath);
        }

        if (!HasProductionArchiveExtension(
            fullArchivePath))
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.InvalidArchiveExtension,
                $"Production Widget archive must end with {ProductionArchiveExtension}.");
        }

        var stageRoot =
            CreateStageRoot();

        var payloadRoot =
            Path.Combine(
                stageRoot,
                "payload");

        Directory.CreateDirectory(
            payloadRoot);

        try
        {
            ExtractArchiveSafely(
                fullArchivePath,
                payloadRoot,
                cancellationToken);

            return await InstallStagedPayloadAsync(
                payloadRoot,
                WidgetPackageSourceMode.Archive,
                cancellationToken);
        }
        catch (
            WidgetPackageValidationException exception)
        {
            QuarantineRejectedArchive(
                fullArchivePath,
                exception);

            throw;
        }
        finally
        {
            DeleteDirectoryIfPresent(
                stageRoot);
        }
    }

    public async Task<WidgetPackageInstallResult> InstallDevelopmentFolderAsync(
        string folderPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            folderPath);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.AllowDevelopmentFolderInstall)
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.DevelopmentFolderInstallDisabled,
                "Development-folder installation is disabled.");
        }

        var fullFolderPath =
            Path.GetFullPath(
                folderPath);

        if (!Directory.Exists(
            fullFolderPath))
        {
            throw new DirectoryNotFoundException(
                fullFolderPath);
        }

        var stageRoot =
            CreateStageRoot();

        var payloadRoot =
            Path.Combine(
                stageRoot,
                "payload");

        Directory.CreateDirectory(
            payloadRoot);

        try
        {
            CopyDevelopmentFolderSafely(
                fullFolderPath,
                payloadRoot,
                cancellationToken);

            return await InstallStagedPayloadAsync(
                payloadRoot,
                WidgetPackageSourceMode.DevelopmentFolder,
                cancellationToken);
        }
        finally
        {
            DeleteDirectoryIfPresent(
                stageRoot);
        }
    }

    private async Task<WidgetPackageInstallResult> InstallStagedPayloadAsync(
        string payloadRoot,
        WidgetPackageSourceMode sourceMode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var manifest =
            await ReadAndValidateManifestAsync(
                payloadRoot,
                cancellationToken);

        var packageVersion =
            ParseVersion(
                manifest.PackageVersion,
                WidgetPackageValidationCode.InvalidPackageVersion,
                "Package version is invalid.");

        var targetPath =
            Path.Combine(
                InstalledDirectory,
                manifest.WidgetId);

        string? backupPath =
            null;

        if (Directory.Exists(
            targetPath))
        {
            var widgetBackupDirectory =
                Path.Combine(
                    BackupDirectory,
                    manifest.WidgetId);

            EnsureDirectory(
                widgetBackupDirectory);

            backupPath =
                Path.Combine(
                    widgetBackupDirectory,
                    DateTimeOffset.UtcNow.ToString(
                        "yyyyMMdd_HHmmss_fff")
                    + "_"
                    + Guid
                        .NewGuid()
                        .ToString(
                            "N"));

            Directory.Move(
                targetPath,
                backupPath);
        }

        try
        {
            Directory.Move(
                payloadRoot,
                targetPath);
        }
        catch
        {
            if (Directory.Exists(
                targetPath))
            {
                DeleteDirectoryIfPresent(
                    targetPath);
            }

            if (
                backupPath is not null
                && Directory.Exists(
                    backupPath)
            )
            {
                Directory.Move(
                    backupPath,
                    targetPath);
            }

            throw;
        }

        return new WidgetPackageInstallResult(
            WidgetId:
                manifest.WidgetId,
            PackageVersion:
                packageVersion,
            SourceMode:
                sourceMode,
            InstalledPath:
                targetPath,
            BackupPath:
                backupPath);
    }

    private async Task<WidgetPackageManifest> ReadAndValidateManifestAsync(
        string payloadRoot,
        CancellationToken cancellationToken)
    {
        var manifestPath =
            Path.Combine(
                payloadRoot,
                ManifestFileName);

        if (!File.Exists(
            manifestPath))
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.MissingManifest,
                $"Widget package is missing root-level {ManifestFileName}.");
        }

        WidgetPackageManifest? manifest;

        try
        {
            await using var stream =
                File.OpenRead(
                    manifestPath);

            manifest =
                await JsonSerializer.DeserializeAsync<
                    WidgetPackageManifest>(
                        stream,
                        _jsonOptions,
                        cancellationToken);
        }
        catch (
            JsonException exception)
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.InvalidManifest,
                $"Widget package manifest is invalid JSON: {exception.Message}");
        }

        if (manifest is null)
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.InvalidManifest,
                "Widget package manifest is empty.");
        }

        if (
            manifest.ManifestSchemaVersion
            != 1
        )
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.UnsupportedManifestSchema,
                "Widget package manifest schema is unsupported.");
        }

        if (
            string.IsNullOrWhiteSpace(
                manifest.WidgetId)
            || !WidgetIdRegex.IsMatch(
                manifest.WidgetId)
        )
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.InvalidWidgetId,
                "Widget ID is invalid.");
        }

        _ =
            ParseVersion(
                manifest.PackageVersion,
                WidgetPackageValidationCode.InvalidPackageVersion,
                "Package version is invalid.");

        var minimumHostVersion =
            ParseVersion(
                manifest.MinimumHostVersion,
                WidgetPackageValidationCode.InvalidMinimumHostVersion,
                "Minimum host version is invalid.");

        if (
            _options.HostVersion
            < minimumHostVersion
        )
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.HostVersionIncompatible,
                "Widget package requires a newer CoreHost version.");
        }

        var normalizedEntryAssembly =
            NormalizeRelativePath(
                manifest.EntryAssembly,
                WidgetPackageValidationCode.InvalidEntryAssembly,
                "Entry assembly path is invalid.");

        if (!normalizedEntryAssembly.EndsWith(
            ".dll",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.InvalidEntryAssembly,
                "Entry assembly must be a DLL path.");
        }

        var entryAssemblyPath =
            GetSafeDestinationPath(
                payloadRoot,
                normalizedEntryAssembly,
                WidgetPackageValidationCode.InvalidEntryAssembly);

        if (!File.Exists(
            entryAssemblyPath))
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.MissingEntryAssembly,
                "Entry assembly does not exist in the staged package.");
        }

        if (string.IsNullOrWhiteSpace(
            manifest.EntryType))
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.MissingEntryType,
                "Entry type is required.");
        }

        foreach (var capability in
            manifest.Capabilities
            ?? Array.Empty<string>())
        {
            if (
                string.IsNullOrWhiteSpace(
                    capability)
                || !WidgetCapabilityCatalog.IsPackageApprovable(
                    capability)
                || !_options.AllowedCapabilities.Contains(
                    capability)
            )
            {
                throw new WidgetPackageValidationException(
                    WidgetPackageValidationCode.UnsupportedCapability,
                    $"Widget capability is not allowed: {capability}");
            }
        }

        manifest.EntryAssembly =
            normalizedEntryAssembly;

        return manifest;
    }

    private void ExtractArchiveSafely(
        string archivePath,
        string payloadRoot,
        CancellationToken cancellationToken)
    {
        using var archive =
            ZipFile.OpenRead(
                archivePath);

        if (
            archive.Entries.Count
            > _options.MaximumArchiveEntries
        )
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.ArchiveEntryLimitExceeded,
                "Widget archive contains too many entries.");
        }

        var seenEntries =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        long expandedBytes =
            0;

        foreach (var entry in
            archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedPath =
                NormalizeRelativePath(
                    entry.FullName,
                    WidgetPackageValidationCode.UnsafeArchiveEntry,
                    "Widget archive entry path is unsafe.");

            if (!seenEntries.Add(
                normalizedPath))
            {
                throw new WidgetPackageValidationException(
                    WidgetPackageValidationCode.DuplicateArchiveEntry,
                    $"Widget archive contains a duplicate path: {normalizedPath}");
            }

            if (
                normalizedPath.EndsWith(
                    "/",
                    StringComparison.Ordinal)
                || normalizedPath.EndsWith(
                    "\\",
                    StringComparison.Ordinal)
            )
            {
                Directory.CreateDirectory(
                    GetSafeDestinationPath(
                        payloadRoot,
                        normalizedPath,
                        WidgetPackageValidationCode.UnsafeArchiveEntry));

                continue;
            }

            expandedBytes =
                checked(
                    expandedBytes
                    + entry.Length);

            if (
                expandedBytes
                > _options.MaximumExpandedBytes
            )
            {
                throw new WidgetPackageValidationException(
                    WidgetPackageValidationCode.ArchiveSizeLimitExceeded,
                    "Widget archive expanded size exceeds the configured limit.");
            }

            var destinationPath =
                GetSafeDestinationPath(
                    payloadRoot,
                    normalizedPath,
                    WidgetPackageValidationCode.UnsafeArchiveEntry);

            var destinationParent =
                Path.GetDirectoryName(
                    destinationPath);

            if (destinationParent is not null)
            {
                Directory.CreateDirectory(
                    destinationParent);
            }

            entry.ExtractToFile(
                destinationPath,
                overwrite:
                    false);
        }
    }

    private void CopyDevelopmentFolderSafely(
        string sourceRoot,
        string destinationRoot,
        CancellationToken cancellationToken)
    {
        var fileCount =
            0;

        long expandedBytes =
            0;

        CopyDirectoryRecursive(
            sourceRoot,
            sourceRoot,
            destinationRoot,
            ref fileCount,
            ref expandedBytes,
            cancellationToken);
    }

    private void CopyDirectoryRecursive(
        string sourceRoot,
        string currentSource,
        string destinationRoot,
        ref int fileCount,
        ref long expandedBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentAttributes =
            File.GetAttributes(
                currentSource);

        if (
            currentAttributes.HasFlag(
                FileAttributes.ReparsePoint)
        )
        {
            throw new WidgetPackageValidationException(
                WidgetPackageValidationCode.UnsafeDevelopmentFolderEntry,
                "Development-folder install rejects reparse points.");
        }

        foreach (var filePath in
            Directory.EnumerateFiles(
                currentSource,
                "*",
                SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attributes =
                File.GetAttributes(
                    filePath);

            if (
                attributes.HasFlag(
                    FileAttributes.ReparsePoint)
            )
            {
                throw new WidgetPackageValidationException(
                    WidgetPackageValidationCode.UnsafeDevelopmentFolderEntry,
                    "Development-folder install rejects reparse points.");
            }

            fileCount++;

            if (
                fileCount
                > _options.MaximumArchiveEntries
            )
            {
                throw new WidgetPackageValidationException(
                    WidgetPackageValidationCode.ArchiveEntryLimitExceeded,
                    "Development folder contains too many files.");
            }

            expandedBytes =
                checked(
                    expandedBytes
                    + new FileInfo(
                        filePath)
                        .Length);

            if (
                expandedBytes
                > _options.MaximumExpandedBytes
            )
            {
                throw new WidgetPackageValidationException(
                    WidgetPackageValidationCode.ArchiveSizeLimitExceeded,
                    "Development folder size exceeds the configured limit.");
            }

            var relativePath =
                Path.GetRelativePath(
                    sourceRoot,
                    filePath);

            var normalizedPath =
                NormalizeRelativePath(
                    relativePath,
                    WidgetPackageValidationCode.UnsafeDevelopmentFolderEntry,
                    "Development-folder entry path is unsafe.");

            var destinationPath =
                GetSafeDestinationPath(
                    destinationRoot,
                    normalizedPath,
                    WidgetPackageValidationCode.UnsafeDevelopmentFolderEntry);

            var destinationParent =
                Path.GetDirectoryName(
                    destinationPath);

            if (destinationParent is not null)
            {
                Directory.CreateDirectory(
                    destinationParent);
            }

            File.Copy(
                filePath,
                destinationPath,
                overwrite:
                    false);
        }

        foreach (var childDirectory in
            Directory.EnumerateDirectories(
                currentSource,
                "*",
                SearchOption.TopDirectoryOnly))
        {
            CopyDirectoryRecursive(
                sourceRoot,
                childDirectory,
                destinationRoot,
                ref fileCount,
                ref expandedBytes,
                cancellationToken);
        }
    }

    private void QuarantineRejectedArchive(
        string archivePath,
        WidgetPackageValidationException exception)
    {
        EnsureDirectory(
            QuarantineDirectory);

        var quarantineBaseName =
            DateTimeOffset.UtcNow.ToString(
                "yyyyMMdd_HHmmss_fff")
            + "_"
            + Guid
                .NewGuid()
                .ToString(
                    "N")
            + "_"
            + Path
                .GetFileName(
                    archivePath);

        var quarantineArchivePath =
            Path.Combine(
                QuarantineDirectory,
                quarantineBaseName);

        var quarantineReasonPath =
            quarantineArchivePath
            + ".reason.txt";

        File.Copy(
            archivePath,
            quarantineArchivePath,
            overwrite:
                false);

        File.WriteAllText(
            quarantineReasonPath,
            $"code={exception.Code}{Environment.NewLine}message={exception.Message}{Environment.NewLine}");
    }

    private string CreateStageRoot()
    {
        EnsureDirectory(
            StagingDirectory);

        var stageRoot =
            Path.Combine(
                StagingDirectory,
                DateTimeOffset.UtcNow.ToString(
                    "yyyyMMdd_HHmmss_fff")
                + "_"
                + Guid
                    .NewGuid()
                    .ToString(
                        "N"));

        Directory.CreateDirectory(
            stageRoot);

        return stageRoot;
    }

    private static string NormalizeRelativePath(
        string value,
        WidgetPackageValidationCode code,
        string message)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            throw new WidgetPackageValidationException(
                code,
                message);
        }

        var normalized =
            value.Replace(
                '\\',
                '/');

        if (
            Path.IsPathRooted(
                normalized)
            || normalized.Contains(':')
            || normalized.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries)
                .Any(
                    segment =>
                        segment
                        == "..")
        )
        {
            throw new WidgetPackageValidationException(
                code,
                message);
        }

        return normalized;
    }

    private static string GetSafeDestinationPath(
        string rootPath,
        string relativePath,
        WidgetPackageValidationCode code)
    {
        var fullRoot =
            Path.GetFullPath(
                rootPath)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        var fullDestination =
            Path.GetFullPath(
                Path.Combine(
                    fullRoot,
                    relativePath));

        if (!fullDestination.StartsWith(
            fullRoot,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new WidgetPackageValidationException(
                code,
                "Widget package path escapes its staging directory.");
        }

        return fullDestination;
    }

    private static Version ParseVersion(
        string value,
        WidgetPackageValidationCode code,
        string message)
    {
        if (!Version.TryParse(
            value,
            out var version))
        {
            throw new WidgetPackageValidationException(
                code,
                message);
        }

        return version;
    }

    private static bool HasProductionArchiveExtension(
        string path)
    {
        return path.EndsWith(
            ProductionArchiveExtension,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureDirectory(
        string path)
    {
        Directory.CreateDirectory(
            path);
    }

    private static void DeleteDirectoryIfPresent(
        string path)
    {
        if (Directory.Exists(
            path))
        {
            Directory.Delete(
                path,
                recursive:
                    true);
        }
    }
}