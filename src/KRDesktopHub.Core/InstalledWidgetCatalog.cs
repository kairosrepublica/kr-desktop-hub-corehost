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

public static class WidgetHostCatalogRefreshAcceptancePolicy
{
    public static bool ShouldApply(
        InstalledWidgetCatalogSnapshot? lastAccepted,
        InstalledWidgetCatalogSnapshot candidate)
    {
        ArgumentNullException.ThrowIfNull(
            candidate);

        if (
            lastAccepted is null
            || candidate.Failures.Count == 0
        )
        {
            return true;
        }

        var candidateIds =
            candidate
                .Widgets
                .Select(
                    widget =>
                        widget.WidgetId)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        return !lastAccepted
            .Widgets
            .Where(
                widget =>
                    widget.Enabled)
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
                RefreshCoreAsync,
                cancellationToken);
    }

    private async Task<InstalledWidgetCatalogSnapshot> RefreshCoreAsync(
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(
            _installedDirectory);

        var items =
            new List<InstalledWidgetCatalogItem>();

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
                            cancellationToken);

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

                var layout =
                    _layoutController
                        .RegisterOrUpdate(
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
                                index * 10));

                var surface =
                    layout
                        .Widgets
                        .Single(
                            widget =>
                                string.Equals(
                                    widget.WidgetId,
                                    manifest.WidgetId,
                                    StringComparison.OrdinalIgnoreCase));

                items.Add(
                    new InstalledWidgetCatalogItem(
                        manifest.WidgetId,
                        manifest.DisplayName,
                        packageVersion,
                        directory,
                        manifest.Capabilities,
                        surface.Enabled,
                        surface.Collapsed,
                        surface.Order,
                        surface.PreferredExpandedHeightDip,
                        surface.MinimumCollapsedHeightDip,
                        surface.MeasuredDesiredHeightDip,
                        surface.ActualHeightDip));
            }
            catch (Exception exception)
            {
                failures.Add(
                    new InstalledWidgetCatalogFailure(
                        directory,
                        exception.Message));
            }
        }

        var snapshot =
            _layoutController.GetLayout();

        return new InstalledWidgetCatalogSnapshot(
            items
                .OrderBy(
                    item =>
                        item.Order)
                .ThenBy(
                    item =>
                        item.WidgetId,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            failures,
            snapshot);
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
