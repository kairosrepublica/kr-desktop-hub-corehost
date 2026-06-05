namespace KRDesktopHub.Core;

public sealed record WidgetInboxArchiveInfo(
    string FileName,
    string FullPath,
    long SizeBytes,
    DateTimeOffset LastWriteTimeUtc)
{
    public string DisplayText =>
        $"{FileName} | {SizeBytes} bytes | {LastWriteTimeUtc:u}";
}

public sealed class InternalWidgetManagerService
{
    private readonly InternalWidgetPackageInstaller _installer;

    public InternalWidgetManagerService(
        InternalWidgetPackageInstaller installer)
    {
        ArgumentNullException.ThrowIfNull(
            installer);

        _installer =
            installer;
    }

    public string PluginsDirectory =>
        _installer.PluginsDirectory;

    public string InboxDirectory =>
        _installer.InboxDirectory;

    public IReadOnlyList<WidgetInboxArchiveInfo> RefreshInbox()
    {
        return _installer
            .DiscoverInboxArchives()
            .Select(
                path =>
                {
                    var info =
                        new FileInfo(
                            path);

                    return new WidgetInboxArchiveInfo(
                        FileName:
                            info.Name,
                        FullPath:
                            info.FullName,
                        SizeBytes:
                            info.Length,
                        LastWriteTimeUtc:
                            info.LastWriteTimeUtc);
                })
            .ToArray();
    }

    public Task<WidgetPackageInstallResult> InstallSelectedArchiveAsync(
        string archivePath,
        CancellationToken cancellationToken)
    {
        return _installer.InstallArchiveAsync(
            archivePath,
            cancellationToken);
    }

    public Task<WidgetPackageInstallResult> InstallInboxArchiveAsync(
        string archivePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            archivePath);

        var fullArchivePath =
            Path.GetFullPath(
                archivePath);

        var fullInboxPath =
            Path.GetFullPath(
                InboxDirectory)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullArchivePath.StartsWith(
            fullInboxPath,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Selected inbox archive is outside plugins/inbox.");
        }

        var relativePath =
            Path.GetRelativePath(
                InboxDirectory,
                fullArchivePath);

        if (!string.IsNullOrWhiteSpace(
            Path.GetDirectoryName(
                relativePath)))
        {
            throw new InvalidOperationException(
                "Inbox installation accepts only top-level discovered archives.");
        }

        if (!RefreshInbox().Any(
            archive =>
                string.Equals(
                    archive.FullPath,
                    fullArchivePath,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Selected inbox archive is not an eligible discovered package.");
        }

        return _installer.InstallArchiveAsync(
            fullArchivePath,
            cancellationToken);
    }

    public Task<WidgetPackageInstallResult> InstallDevelopmentFolderAsync(
        string folderPath,
        CancellationToken cancellationToken)
    {
        return _installer.InstallDevelopmentFolderAsync(
            folderPath,
            cancellationToken);
    }
}