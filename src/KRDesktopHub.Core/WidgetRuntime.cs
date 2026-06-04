using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using KRDesktopHub.Contracts;

namespace KRDesktopHub.Core;

public sealed class WidgetManifest
{
    public string WidgetId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string WidgetVersion { get; init; } = string.Empty;

    public string RequiredContractsVersion { get; init; } = string.Empty;

    public string MinimumHostVersion { get; init; } = string.Empty;

    public string EntryAssembly { get; init; } = string.Empty;

    public string EntryType { get; init; } = string.Empty;

    public WidgetActivationMode ActivationMode { get; init; }

    public IReadOnlyList<string> Capabilities { get; init; } =
        Array.Empty<string>();
}

public static class WidgetManifestValidator
{
    public static void Validate(
        WidgetManifest manifest,
        string pluginDirectory,
        Version hostVersion,
        Version contractsVersion)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDirectory);
        ArgumentNullException.ThrowIfNull(hostVersion);
        ArgumentNullException.ThrowIfNull(contractsVersion);

        if (string.IsNullOrWhiteSpace(manifest.WidgetId) ||
            !manifest.WidgetId.All(
                character =>
                    char.IsLetterOrDigit(character)
                    || character is '.' or '-' or '_'))
        {
            throw new InvalidOperationException(
                "Widget ID contains unsupported characters.");
        }

        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
        {
            throw new InvalidOperationException(
                "Widget display name is required.");
        }

        var widgetVersion =
            ParseVersion(
                manifest.WidgetVersion,
                nameof(manifest.WidgetVersion));

        var requiredContractsVersion =
            ParseVersion(
                manifest.RequiredContractsVersion,
                nameof(manifest.RequiredContractsVersion));

        var minimumHostVersion =
            ParseVersion(
                manifest.MinimumHostVersion,
                nameof(manifest.MinimumHostVersion));

        if (widgetVersion <= new Version(0, 0))
        {
            throw new InvalidOperationException(
                "Widget version must be greater than zero.");
        }

        if (requiredContractsVersion > contractsVersion)
        {
            throw new InvalidOperationException(
                $"Widget requires Contracts {requiredContractsVersion}; Host provides {contractsVersion}.");
        }

        if (minimumHostVersion > hostVersion)
        {
            throw new InvalidOperationException(
                $"Widget requires Host {minimumHostVersion}; Host provides {hostVersion}.");
        }

        if (string.IsNullOrWhiteSpace(manifest.EntryAssembly))
        {
            throw new InvalidOperationException(
                "Widget entry assembly is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.EntryType))
        {
            throw new InvalidOperationException(
                "Widget entry type is required.");
        }

        var root =
            Path.GetFullPath(pluginDirectory)
            + Path.DirectorySeparatorChar;

        var assemblyPath =
            Path.GetFullPath(
                Path.Combine(
                    pluginDirectory,
                    manifest.EntryAssembly));

        if (!assemblyPath.StartsWith(
            root,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Widget entry assembly must remain inside the plugin directory.");
        }

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException(
                "Widget entry assembly was not found.",
                assemblyPath);
        }
    }

    private static Version ParseVersion(
        string value,
        string fieldName)
    {
        return Version.TryParse(
            value,
            out var version)
                ? version
                : throw new InvalidOperationException(
                    $"Invalid version in {fieldName}: {value}");
    }
}

public sealed class WidgetPluginLoadContext
    : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public WidgetPluginLoadContext(
        string mainAssemblyPath)
        : base(
            name:
                $"KRDesktopHub.Widget.{Path.GetFileNameWithoutExtension(mainAssemblyPath)}",

            isCollectible:
                true)
    {
        _resolver =
            new AssemblyDependencyResolver(
                mainAssemblyPath);
    }

    protected override Assembly? Load(
        AssemblyName assemblyName)
    {
        var contractsAssemblyName =
            typeof(IKrWidget)
                .Assembly
                .GetName()
                .Name;

        if (string.Equals(
            assemblyName.Name,
            contractsAssemblyName,
            StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path =
            _resolver.ResolveAssemblyToPath(
                assemblyName);

        return path is null
            ? null
            : LoadFromAssemblyPath(path);
    }

    protected override IntPtr LoadUnmanagedDll(
        string unmanagedDllName)
    {
        var path =
            _resolver.ResolveUnmanagedDllToPath(
                unmanagedDllName);

        return path is null
            ? IntPtr.Zero
            : LoadUnmanagedDllFromPath(path);
    }
}

public sealed class LoadedWidget : IDisposable
{
    private bool _disposed;

    public LoadedWidget(
        WidgetManifest manifest,
        IKrWidget widget,
        WidgetPluginLoadContext loadContext)
    {
        Manifest = manifest;
        Widget = widget;
        LoadContext = loadContext;
    }

    public WidgetManifest Manifest { get; }

    public IKrWidget Widget { get; }

    public WidgetPluginLoadContext LoadContext { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (Widget is IDisposable disposable)
        {
            disposable.Dispose();
        }

        LoadContext.Unload();
        _disposed = true;
    }
}

public sealed record WidgetDiscoveryFailure(
    string PluginDirectory,
    string Error);

public sealed record WidgetDiscoveryResult(
    IReadOnlyList<LoadedWidget> Widgets,
    IReadOnlyList<WidgetDiscoveryFailure> Failures);

public sealed class WidgetPluginLoader
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    private readonly Version _hostVersion;
    private readonly Version _contractsVersion;

    public WidgetPluginLoader(
        Version hostVersion)
    {
        _hostVersion = hostVersion;

        _contractsVersion =
            typeof(IKrWidget)
                .Assembly
                .GetName()
                .Version
            ?? new Version(1, 0, 0, 0);
    }

    public async Task<LoadedWidget> LoadAsync(
        string pluginDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            pluginDirectory);

        var manifestPath =
            Path.Combine(
                pluginDirectory,
                "manifest.json");

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException(
                "Widget manifest was not found.",
                manifestPath);
        }

        await using var stream =
            File.OpenRead(
                manifestPath);

        var manifest =
            await JsonSerializer.DeserializeAsync<WidgetManifest>(
                stream,
                JsonOptions,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Widget manifest could not be parsed.");

        WidgetManifestValidator.Validate(
            manifest,
            pluginDirectory,
            _hostVersion,
            _contractsVersion);

        var assemblyPath =
            Path.GetFullPath(
                Path.Combine(
                    pluginDirectory,
                    manifest.EntryAssembly));

        var loadContext =
            new WidgetPluginLoadContext(
                assemblyPath);

        try
        {
            var assembly =
                loadContext.LoadFromAssemblyPath(
                    assemblyPath);

            var type =
                assembly.GetType(
                    manifest.EntryType,
                    throwOnError: true,
                    ignoreCase: false)
                ?? throw new InvalidOperationException(
                    $"Widget entry type was not found: {manifest.EntryType}");

            if (!typeof(IKrWidget).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(
                    $"Widget entry type does not implement {nameof(IKrWidget)}.");
            }

            var widget =
                Activator.CreateInstance(type)
                    as IKrWidget
                ?? throw new InvalidOperationException(
                    "Widget entry type could not be constructed.");

            if (!string.Equals(
                widget.Descriptor.WidgetId,
                manifest.WidgetId,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Widget descriptor ID does not match manifest ID.");
            }

            return new LoadedWidget(
                manifest,
                widget,
                loadContext);
        }
        catch
        {
            loadContext.Unload();
            throw;
        }
    }

    public async Task<WidgetDiscoveryResult> DiscoverAsync(
        string pluginRoot,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            pluginRoot);

        Directory.CreateDirectory(
            pluginRoot);

        var widgets =
            new List<LoadedWidget>();

        var failures =
            new List<WidgetDiscoveryFailure>();

        foreach (var directory in
            Directory.GetDirectories(pluginRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                widgets.Add(
                    await LoadAsync(
                        directory,
                        cancellationToken));
            }
            catch (Exception exception)
            {
                failures.Add(
                    new WidgetDiscoveryFailure(
                        directory,
                        exception.Message));
            }
        }

        return new WidgetDiscoveryResult(
            widgets,
            failures);
    }
}

public sealed class PeriodicWidgetScheduler
    : IWidgetScheduler, IAsyncDisposable
{
    private readonly ConcurrentDictionary<
        string,
        ScheduledJob> _jobs =
            new(StringComparer.OrdinalIgnoreCase);

    public Task ScheduleAsync(
        string jobId,
        TimeSpan interval,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        ArgumentNullException.ThrowIfNull(action);

        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                "Interval must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var source =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        var task =
            Task.Run(
                () =>
                    RunPeriodicAsync(
                        interval,
                        action,
                        source.Token),

                CancellationToken.None);

        var job =
            new ScheduledJob(
                source,
                task);

        if (!_jobs.TryAdd(jobId, job))
        {
            source.Cancel();
            source.Dispose();

            throw new InvalidOperationException(
                $"Scheduled job already exists: {jobId}");
        }

        return Task.CompletedTask;
    }

    public async Task CancelAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_jobs.TryRemove(
            jobId,
            out var job))
        {
            return;
        }

        job.Source.Cancel();

        try
        {
            await job.Task;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            job.Source.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var jobId in
            _jobs.Keys.ToArray())
        {
            await CancelAsync(
                jobId,
                CancellationToken.None);
        }
    }

    private static async Task RunPeriodicAsync(
        TimeSpan interval,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        using var timer =
            new PeriodicTimer(
                interval);

        while (await timer.WaitForNextTickAsync(
            cancellationToken))
        {
            await action(
                cancellationToken);
        }
    }

    private sealed record ScheduledJob(
        CancellationTokenSource Source,
        Task Task);
}

public sealed class NullWidgetNotificationClient
    : IWidgetNotificationClient
{
    public Task PublishAsync(
        WidgetNotification notification,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(notification);

        return Task.CompletedTask;
    }
}

public sealed record DefaultWidgetContext(
    IWidgetLogger Logger,
    IWidgetScheduler Scheduler,
    IWidgetStateStore StateStore,
    IWidgetSettingsStore SettingsStore,
    IEventBus EventBus,
    ICommandRegistry Commands,
    IClock Clock,
    ILocalizationService Localization,
    IWidgetNotificationClient Notifications)
    : IWidgetContext;

public sealed record WidgetRuntimePolicy(
    int MaxRetries,
    int QuarantineAfterFailedCycles,
    TimeSpan OperationTimeout,
    int MaxConcurrentOperations)
{
    public static WidgetRuntimePolicy Default =>
        new(
            MaxRetries:
                5,

            QuarantineAfterFailedCycles:
                5,

            OperationTimeout:
                TimeSpan.FromSeconds(30),

            MaxConcurrentOperations:
                10);

    public void Validate()
    {
        if (MaxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxRetries));
        }

        if (QuarantineAfterFailedCycles <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(QuarantineAfterFailedCycles));
        }

        if (OperationTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(OperationTimeout));
        }

        if (MaxConcurrentOperations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxConcurrentOperations));
        }
    }
}

public sealed record WidgetRuntimeSnapshot(
    string WidgetId,
    WidgetRuntimeState State,
    int ConsecutiveFailedCycles,
    DateTimeOffset? LastSuccessAtUtc,
    DateTimeOffset? LastFailureAtUtc,
    string? LastError);

public sealed class WidgetRuntimeController
    : IAsyncDisposable
{
    private readonly WidgetRuntimePolicy _policy;
    private readonly SemaphoreSlim _concurrencyGate;
    private readonly ConcurrentDictionary<
        string,
        WidgetEntry> _widgets =
            new(StringComparer.OrdinalIgnoreCase);

    public WidgetRuntimeController(
        WidgetRuntimePolicy policy)
    {
        policy.Validate();

        _policy = policy;

        _concurrencyGate =
            new SemaphoreSlim(
                policy.MaxConcurrentOperations,
                policy.MaxConcurrentOperations);
    }

    public void Register(
        IKrWidget widget)
    {
        ArgumentNullException.ThrowIfNull(widget);

        if (!_widgets.TryAdd(
            widget.Descriptor.WidgetId,
            new WidgetEntry(widget)))
        {
            throw new InvalidOperationException(
                $"Widget already registered: {widget.Descriptor.WidgetId}");
        }
    }

    public WidgetRuntimeSnapshot GetSnapshot(
        string widgetId)
    {
        return GetEntry(widgetId)
            .ToSnapshot();
    }

    public Task InitializeAsync(
        string widgetId,
        IWidgetContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return RunLifecycleAsync(
            widgetId,
            startingState:
                WidgetRuntimeState.Starting,

            successState:
                WidgetRuntimeState.ScheduledInactive,

            action:
                (widget, token) =>
                    widget.InitializeAsync(
                        context,
                        token),

            cancellationToken);
    }

    public Task StartAsync(
        string widgetId,
        CancellationToken cancellationToken)
    {
        return RunLifecycleAsync(
            widgetId,
            startingState:
                WidgetRuntimeState.Starting,

            successState:
                WidgetRuntimeState.Running,

            action:
                (widget, token) =>
                    widget.StartAsync(token),

            cancellationToken);
    }

    public Task PauseAsync(
        string widgetId,
        CancellationToken cancellationToken)
    {
        return RunLifecycleAsync(
            widgetId,
            startingState:
                WidgetRuntimeState.Paused,

            successState:
                WidgetRuntimeState.Paused,

            action:
                (widget, token) =>
                    widget.PauseAsync(token),

            cancellationToken);
    }

    public Task ResumeAsync(
        string widgetId,
        CancellationToken cancellationToken)
    {
        return RunLifecycleAsync(
            widgetId,
            startingState:
                WidgetRuntimeState.Starting,

            successState:
                WidgetRuntimeState.Running,

            action:
                (widget, token) =>
                    widget.ResumeAsync(token),

            cancellationToken);
    }

    public Task StopAsync(
        string widgetId,
        CancellationToken cancellationToken)
    {
        return RunLifecycleAsync(
            widgetId,
            startingState:
                WidgetRuntimeState.Stopping,

            successState:
                WidgetRuntimeState.Disabled,

            action:
                (widget, token) =>
                    widget.StopAsync(token),

            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _concurrencyGate.Dispose();

        foreach (var entry in
            _widgets.Values)
        {
            if (entry.Widget is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        await Task.CompletedTask;
    }

    private async Task RunLifecycleAsync(
        string widgetId,
        WidgetRuntimeState startingState,
        WidgetRuntimeState successState,
        Func<IKrWidget, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var entry =
            GetEntry(widgetId);

        await entry.Gate.WaitAsync(
            cancellationToken);

        try
        {
            if (entry.State ==
                WidgetRuntimeState.Quarantined)
            {
                throw new InvalidOperationException(
                    $"Widget is quarantined: {widgetId}");
            }

            entry.State =
                startingState;

            await _concurrencyGate.WaitAsync(
                cancellationToken);

            try
            {
                await RunWithRetryAsync(
                    entry,
                    action,
                    cancellationToken);
            }
            finally
            {
                _concurrencyGate.Release();
            }

            entry.State =
                successState;

            entry.ConsecutiveFailedCycles =
                0;

            entry.LastSuccessAtUtc =
                DateTimeOffset.UtcNow;

            entry.LastError =
                null;
        }
        catch (Exception exception)
        {
            entry.ConsecutiveFailedCycles++;

            entry.LastFailureAtUtc =
                DateTimeOffset.UtcNow;

            entry.LastError =
                exception.Message;

            entry.State =
                entry.ConsecutiveFailedCycles
                >= _policy.QuarantineAfterFailedCycles
                    ? WidgetRuntimeState.Quarantined
                    : WidgetRuntimeState.Failed;

            throw;
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    private async Task RunWithRetryAsync(
        WidgetEntry entry,
        Func<IKrWidget, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        Exception? lastError =
            null;

        for (var attempt = 0;
            attempt <= _policy.MaxRetries;
            attempt++)
        {
            using var linkedSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            try
            {
                await action(
                    entry.Widget,
                    linkedSource.Token)
                    .WaitAsync(
                        _policy.OperationTimeout,
                        cancellationToken);

                return;
            }
            catch (Exception exception)
                when (
                    exception is not OperationCanceledException
                    || !cancellationToken.IsCancellationRequested)
            {
                linkedSource.Cancel();
                lastError = exception;
            }
        }

        throw new InvalidOperationException(
            $"Widget operation failed after {_policy.MaxRetries + 1} attempt(s).",
            lastError);
    }

    private WidgetEntry GetEntry(
        string widgetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        return _widgets.TryGetValue(
            widgetId,
            out var entry)
                ? entry
                : throw new KeyNotFoundException(
                    $"Widget not registered: {widgetId}");
    }

    private sealed class WidgetEntry
    {
        public WidgetEntry(
            IKrWidget widget)
        {
            Widget = widget;
        }

        public IKrWidget Widget { get; }

        public SemaphoreSlim Gate { get; } =
            new(1, 1);

        public WidgetRuntimeState State { get; set; } =
            WidgetRuntimeState.Discovered;

        public int ConsecutiveFailedCycles { get; set; }

        public DateTimeOffset? LastSuccessAtUtc { get; set; }

        public DateTimeOffset? LastFailureAtUtc { get; set; }

        public string? LastError { get; set; }

        public WidgetRuntimeSnapshot ToSnapshot()
        {
            return new WidgetRuntimeSnapshot(
                Widget.Descriptor.WidgetId,
                State,
                ConsecutiveFailedCycles,
                LastSuccessAtUtc,
                LastFailureAtUtc,
                LastError);
        }
    }
}