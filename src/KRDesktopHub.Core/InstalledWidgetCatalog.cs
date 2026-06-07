using System.Text.Json;
using System.Text.Json.Serialization;
using KRDesktopHub.Contracts;

namespace KRDesktopHub.Core;

public sealed record InstalledWidgetCatalogFailure(
    string InstalledPath,
    string Error);

public sealed record InstalledWidgetCatalogItem(
    string WidgetId,
    string DisplayName,
    Version PackageVersion,
    string InstalledPath,
    IReadOnlyList<string> Capabilities,
    bool Enabled,
    bool Collapsed,
    int Order,
    double PreferredExpandedHeightDip,
    double MinimumCollapsedHeightDip,
    double MeasuredDesiredHeightDip,
    double ActualHeightDip)
{
    public string DisplayText =>
        $"{DisplayName} | {WidgetId} | v{PackageVersion} | "
        + (Enabled
            ? "Enabled"
            : "Disabled")
        + " | "
        + (Collapsed
            ? "Collapsed"
            : "Expanded");
}

public sealed record InstalledWidgetCatalogSnapshot(
    IReadOnlyList<InstalledWidgetCatalogItem> Widgets,
    IReadOnlyList<InstalledWidgetCatalogFailure> Failures,
    WidgetHostLayoutSnapshot Layout);

public sealed record InstalledWidgetCatalogCandidateItem(
    string WidgetId,
    string DisplayName,
    Version PackageVersion,
    string InstalledPath,
    IReadOnlyList<string> Capabilities,
    WidgetHostRegistration Registration);

public sealed record InstalledWidgetCatalogCandidate(
    IReadOnlyList<InstalledWidgetCatalogCandidateItem> Widgets,
    IReadOnlyList<InstalledWidgetCatalogFailure> Failures);

public static class WidgetHostCatalogRefreshAcceptancePolicy
{
    public static bool ShouldApply(
        InstalledWidgetCatalogSnapshot? lastAccepted,
        InstalledWidgetCatalogSnapshot candidate)
    {
        ArgumentNullException.ThrowIfNull(
            candidate);

        return ShouldApplyCore(
            lastAccepted,
            candidate
                .Widgets
                .Select(
                    widget =>
                        widget.WidgetId),
            candidate.Failures.Count);
    }

    public static bool ShouldApply(
        InstalledWidgetCatalogSnapshot? lastAccepted,
        InstalledWidgetCatalogCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(
            candidate);

        return ShouldApplyCore(
            lastAccepted,
            candidate
                .Widgets
                .Select(
                    widget =>
                        widget.WidgetId),
            candidate.Failures.Count);
    }

    private static bool ShouldApplyCore(
        InstalledWidgetCatalogSnapshot? lastAccepted,
        IEnumerable<string> candidateWidgetIds,
        int failureCount)
    {
        ArgumentNullException.ThrowIfNull(
            candidateWidgetIds);

        if (
            lastAccepted is null
            || failureCount == 0
        )
        {
            return true;
        }

        var candidateIds =
            candidateWidgetIds
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        return !lastAccepted
            .Widgets
            .Any(
                widget =>
                    !candidateIds.Contains(
                        widget.WidgetId));
    }
}

public sealed class InstalledWidgetManifestAdapter
{
    private static readonly JsonSerializerOptions RuntimeJsonOptions =
        new()
        {
            PropertyNameCaseInsensitive =
                true,

            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    private static readonly JsonSerializerOptions PackageJsonOptions =
        new()
        {
            PropertyNameCaseInsensitive =
                false,

            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    public async Task<WidgetManifest> ReadRuntimeManifestAsync(
        string installedWidgetDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            installedWidgetDirectory);

        var manifestPath =
            Path.Combine(
                installedWidgetDirectory,
                InternalWidgetPackageInstaller.ManifestFileName);

        if (!File.Exists(
            manifestPath))
        {
            throw new FileNotFoundException(
                "Installed Widget manifest was not found.",
                manifestPath);
        }

        await using var stream =
            File.OpenRead(
                manifestPath);

        using var document =
            await JsonDocument.ParseAsync(
                stream,
                cancellationToken:
                    cancellationToken);

        if (document.RootElement.TryGetProperty(
            "manifest_schema_version",
            out _))
        {
            var packageManifest =
                document.RootElement.Deserialize<
                    WidgetPackageManifest>(
                        PackageJsonOptions)
                ?? throw new InvalidOperationException(
                    "Installed Widget package manifest could not be parsed.");

            return Adapt(
                packageManifest);
        }

        return document.RootElement.Deserialize<
                WidgetManifest>(
                    RuntimeJsonOptions)
            ?? throw new InvalidOperationException(
                "Widget runtime manifest could not be parsed.");
    }

    public WidgetManifest Adapt(
        WidgetPackageManifest packageManifest)
    {
        ArgumentNullException.ThrowIfNull(
            packageManifest);

        return new WidgetManifest
        {
            WidgetId =
                packageManifest.WidgetId,

            DisplayName =
                string.IsNullOrWhiteSpace(
                    packageManifest.DisplayName)
                    ? packageManifest.WidgetId
                    : packageManifest.DisplayName,

            WidgetVersion =
                packageManifest.PackageVersion,

            RequiredContractsVersion =
                string.IsNullOrWhiteSpace(
                    packageManifest.RequiredContractsVersion)
                    ? "1.0.0"
                    : packageManifest.RequiredContractsVersion,

            MinimumHostVersion =
                packageManifest.MinimumHostVersion,

            EntryAssembly =
                packageManifest.EntryAssembly,

            EntryType =
                packageManifest.EntryType,

            ActivationMode =
                packageManifest.ActivationMode,

            Capabilities =
                packageManifest.Capabilities
                ?? Array.Empty<string>(),

            DefaultEnabled =
                packageManifest.DefaultEnabled,

            DefaultCollapsed =
                packageManifest.DefaultCollapsed,

            PreferredExpandedHeightDip =
                packageManifest.PreferredExpandedHeightDip,

            MinimumCollapsedHeightDip =
                packageManifest.MinimumCollapsedHeightDip,

            SettingsSchemaVersion =
                packageManifest.SettingsSchemaVersion,

            StateSchemaVersion =
                packageManifest.StateSchemaVersion
        };
    }
}

public sealed class InstalledWidgetCatalogService
{
    private readonly string _installedDirectory;
    private readonly InstalledWidgetManifestAdapter _manifestAdapter;
    private readonly WidgetHostLayoutController _layoutController;

    private readonly WidgetHostOperationSerialQueue _refreshQueue =
        new();

    public InstalledWidgetCatalogService(
        string installedDirectory,
        WidgetHostLayoutController layoutController,
        InstalledWidgetManifestAdapter? manifestAdapter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            installedDirectory);

        _installedDirectory =
            Path.GetFullPath(
                installedDirectory);

        _layoutController =
            layoutController
            ?? throw new ArgumentNullException(
                nameof(layoutController));

        _manifestAdapter =
            manifestAdapter
            ?? new InstalledWidgetManifestAdapter();
    }

    public string InstalledDirectory =>
        _installedDirectory;

    public WidgetHostLayoutController LayoutController =>
        _layoutController;

    public Task<InstalledWidgetCatalogSnapshot> RefreshAsync(
        CancellationToken cancellationToken)
    {
        return _refreshQueue
            .RunAsync(
                async innerCancellationToken =>
                {
                    var candidate =
                        await DiscoverCoreAsync(
                                innerCancellationToken)
                            .ConfigureAwait(
                                false);

                    return CommitAcceptedCandidate(
                        candidate);
                },
                cancellationToken);
    }

    public Task<InstalledWidgetCatalogCandidate> DiscoverAsync(
        CancellationToken cancellationToken)
    {
        return _refreshQueue
            .RunAsync(
                DiscoverCoreAsync,
                cancellationToken);
    }

    public InstalledWidgetCatalogSnapshot CommitAcceptedCandidate(
        InstalledWidgetCatalogCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(
            candidate);

        var layout =
            _layoutController
                .ReconcileActiveRegistrations(
                    candidate
                        .Widgets
                        .Select(
                            widget =>
                                widget.Registration));

        return MaterializeSnapshot(
            candidate,
            layout);
    }

    private async Task<InstalledWidgetCatalogCandidate> DiscoverCoreAsync(
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(
            _installedDirectory);

        var items =
            new List<InstalledWidgetCatalogCandidateItem>();

        var failures =
            new List<InstalledWidgetCatalogFailure>();

        var directories =
            Directory
                .EnumerateDirectories(
                    _installedDirectory,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .OrderBy(
                    path =>
                        path,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        for (var index = 0;
            index < directories.Length;
            index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory =
                directories[index];

            try
            {
                var manifest =
                    await _manifestAdapter
                        .ReadRuntimeManifestAsync(
                            directory,
                            cancellationToken)
                        .ConfigureAwait(
                            false);

                if (!string.Equals(
                    Path.GetFileName(
                        directory),
                    manifest.WidgetId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Installed Widget folder name does not match manifest Widget ID.");
                }

                var packageVersion =
                    ParsePackageVersion(
                        manifest.WidgetVersion,
                        manifest.WidgetId);

                items.Add(
                    new InstalledWidgetCatalogCandidateItem(
                        manifest.WidgetId,
                        manifest.DisplayName,
                        packageVersion,
                        directory,
                        manifest.Capabilities,
                        new WidgetHostRegistration(
                            manifest.WidgetId,
                            manifest.DisplayName,
                            new WidgetPresentationMetadata(
                                manifest.DefaultEnabled,
                                manifest.DefaultCollapsed,
                                manifest.PreferredExpandedHeightDip,
                                manifest.MinimumCollapsedHeightDip,
                                manifest.SettingsSchemaVersion,
                                manifest.StateSchemaVersion),
                            index * 10)));
            }
            catch (Exception exception)
            {
                failures.Add(
                    new InstalledWidgetCatalogFailure(
                        directory,
                        exception.Message));
            }
        }

        return new InstalledWidgetCatalogCandidate(
            items,
            failures);
    }

    private static InstalledWidgetCatalogSnapshot MaterializeSnapshot(
        InstalledWidgetCatalogCandidate candidate,
        WidgetHostLayoutSnapshot layout)
    {
        ArgumentNullException.ThrowIfNull(
            candidate);

        ArgumentNullException.ThrowIfNull(
            layout);

        var layoutByWidgetId =
            layout
                .Widgets
                .ToDictionary(
                    widget =>
                        widget.WidgetId,
                    StringComparer.OrdinalIgnoreCase);

        var items =
            candidate
                .Widgets
                .Select(
                    widget =>
                    {
                        if (!layoutByWidgetId.TryGetValue(
                            widget.WidgetId,
                            out var surface))
                        {
                            throw new InvalidOperationException(
                                $"Accepted Widget is missing from the committed framework layout: {widget.WidgetId}");
                        }

                        return new InstalledWidgetCatalogItem(
                            widget.WidgetId,
                            widget.DisplayName,
                            widget.PackageVersion,
                            widget.InstalledPath,
                            widget.Capabilities,
                            surface.Enabled,
                            surface.Collapsed,
                            surface.Order,
                            surface.PreferredExpandedHeightDip,
                            surface.MinimumCollapsedHeightDip,
                            surface.MeasuredDesiredHeightDip,
                            surface.ActualHeightDip);
                    })
                .OrderBy(
                    item =>
                        item.Order)
                .ThenBy(
                    item =>
                        item.WidgetId,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return new InstalledWidgetCatalogSnapshot(
            items,
            candidate.Failures,
            layout);
    }

    public WidgetHostLayoutSnapshot SetEnabled(
        string widgetId,
        bool enabled)
    {
        return _layoutController
            .SetEnabled(
                widgetId,
                enabled);
    }

    public WidgetHostLayoutSnapshot SetCollapsed(
        string widgetId,
        bool collapsed)
    {
        return _layoutController
            .SetCollapsed(
                widgetId,
                collapsed);
    }

    public WidgetHostLayoutSnapshot SetOrder(
        string widgetId,
        int order)
    {
        return _layoutController
            .SetOrder(
                widgetId,
                order);
    }

    public WidgetHostLayoutSnapshot GetLayout()
    {
        return _layoutController
            .GetLayout();
    }

    private static Version ParsePackageVersion(
        string value,
        string widgetId)
    {
        return Version.TryParse(
            value,
            out var version)
                ? version
                : throw new InvalidOperationException(
                    $"Installed Widget package version is invalid for {widgetId}: {value}");
    }
}
