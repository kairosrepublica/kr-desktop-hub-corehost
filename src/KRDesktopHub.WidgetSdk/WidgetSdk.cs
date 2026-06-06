using System.Text.Json;
using System.Text.Json.Serialization;
using KRDesktopHub.Contracts;

namespace KRDesktopHub.WidgetSdk;

public abstract class KrWidgetBase
    : IKrWidget
{
    private IWidgetContext? _context;

    public abstract WidgetDescriptor Descriptor { get; }

    protected IWidgetContext Context =>
        _context
        ?? throw new InvalidOperationException(
            "Widget has not been initialized.");

    public async Task InitializeAsync(
        IWidgetContext context,
        CancellationToken cancellationToken)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));

        await OnInitializeAsync(
            cancellationToken);
    }

    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        return OnStartAsync(
            cancellationToken);
    }

    public Task PauseAsync(
        CancellationToken cancellationToken)
    {
        return OnPauseAsync(
            cancellationToken);
    }

    public Task ResumeAsync(
        CancellationToken cancellationToken)
    {
        return OnResumeAsync(
            cancellationToken);
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return OnStopAsync(
            cancellationToken);
    }

    protected virtual Task OnInitializeAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnStartAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnPauseAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnResumeAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnStopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected Task<T> GetSettingAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken)
    {
        return Context
            .SettingsStore
            .GetAsync(
                key,
                defaultValue,
                cancellationToken);
    }

    protected Task<T?> ReadStateAsync<T>(
        string key,
        CancellationToken cancellationToken)
    {
        return Context
            .StateStore
            .ReadAsync<T>(
                key,
                cancellationToken);
    }

    protected Task WriteStateAsync<T>(
        string key,
        T value,
        CancellationToken cancellationToken)
    {
        return Context
            .StateStore
            .WriteAsync(
                key,
                value,
                cancellationToken);
    }
}

public sealed record WidgetManifestDocument(
    [property: JsonPropertyName("widgetId")]
    string WidgetId,

    [property: JsonPropertyName("displayName")]
    string DisplayName,

    [property: JsonPropertyName("widgetVersion")]
    string WidgetVersion,

    [property: JsonPropertyName("requiredContractsVersion")]
    string RequiredContractsVersion,

    [property: JsonPropertyName("minimumHostVersion")]
    string MinimumHostVersion,

    [property: JsonPropertyName("entryAssembly")]
    string EntryAssembly,

    [property: JsonPropertyName("entryType")]
    string EntryType,

    [property: JsonPropertyName("activationMode")]
    WidgetActivationMode ActivationMode,

    [property: JsonPropertyName("capabilities")]
    IReadOnlyList<string> Capabilities,

    [property: JsonPropertyName("defaultEnabled")]
    bool DefaultEnabled,

    [property: JsonPropertyName("defaultCollapsed")]
    bool DefaultCollapsed,

    [property: JsonPropertyName("preferredExpandedHeightDip")]
    double PreferredExpandedHeightDip,

    [property: JsonPropertyName("minimumCollapsedHeightDip")]
    double MinimumCollapsedHeightDip,

    [property: JsonPropertyName("settingsSchemaVersion")]
    int SettingsSchemaVersion,

    [property: JsonPropertyName("stateSchemaVersion")]
    int StateSchemaVersion);

public static class WidgetManifestFile
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    public static WidgetManifestDocument Create(
        IKrWidget widget,
        string entryAssembly,
        string entryType,
        WidgetActivationMode activationMode,
        WidgetPresentationMetadata? presentation = null)
    {
        ArgumentNullException.ThrowIfNull(
            widget);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            entryAssembly);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            entryType);

        var descriptor =
            widget.Descriptor;

        presentation ??=
            new WidgetPresentationMetadata(
                DefaultEnabled:
                    true,
                DefaultCollapsed:
                    false,
                PreferredExpandedHeightDip:
                    220,
                MinimumCollapsedHeightDip:
                    44,
                SettingsSchemaVersion:
                    1,
                StateSchemaVersion:
                    1);

        return new WidgetManifestDocument(
            descriptor.WidgetId,
            descriptor.DisplayName,
            descriptor.WidgetVersion.ToString(3),
            descriptor.RequiredContractsVersion.ToString(3),
            descriptor.MinimumHostVersion.ToString(3),
            entryAssembly,
            entryType,
            activationMode,
            descriptor.Capabilities,
            presentation.DefaultEnabled,
            presentation.DefaultCollapsed,
            presentation.PreferredExpandedHeightDip,
            presentation.MinimumCollapsedHeightDip,
            presentation.SettingsSchemaVersion,
            presentation.StateSchemaVersion);
    }

    public static async Task WriteAsync(
        string path,
        WidgetManifestDocument manifest,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        ArgumentNullException.ThrowIfNull(
            manifest);

        var directory =
            Path.GetDirectoryName(
                path);

        if (!string.IsNullOrWhiteSpace(
            directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(
                manifest,
                JsonOptions),

            cancellationToken);
    }
}
