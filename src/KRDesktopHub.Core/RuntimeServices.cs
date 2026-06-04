using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using KRDesktopHub.Contracts;

namespace KRDesktopHub.Core;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class ConsoleWidgetLogger : IWidgetLogger
{
    public void Information(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }

    public void Warning(string message)
    {
        Console.WriteLine($"[WARN] {message}");
    }

    public void Error(string message, Exception? exception = null)
    {
        Console.WriteLine($"[ERROR] {message}");

        if (exception is not null)
        {
            Console.WriteLine(exception);
        }
    }
}

public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<
        Type,
        ConcurrentDictionary<Guid, Func<object, CancellationToken, Task>>> _handlers = new();

    public Task PublishAsync<TEvent>(
        TEvent eventPayload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventPayload);

        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(
            handlers.Values.Select(
                handler => handler(eventPayload, cancellationToken)));
    }

    public IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var handlers = _handlers.GetOrAdd(
            typeof(TEvent),
            _ => new ConcurrentDictionary<Guid, Func<object, CancellationToken, Task>>());

        var subscriptionId = Guid.NewGuid();

        handlers[subscriptionId] =
            (payload, cancellationToken) =>
                handler((TEvent)payload, cancellationToken);

        return new Subscription(
            () => handlers.TryRemove(subscriptionId, out _));
    }

    private sealed class Subscription : IDisposable
    {
        private Action? _dispose;

        public Subscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }
}

public sealed class CommandRegistry : ICommandRegistry
{
    private readonly ConcurrentDictionary<
        string,
        Func<CancellationToken, Task>> _handlers =
            new(StringComparer.OrdinalIgnoreCase);

    public void Register(
        WidgetCommand command,
        Func<CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(handler);

        if (string.IsNullOrWhiteSpace(command.CommandId))
        {
            throw new ArgumentException(
                "Command ID must not be empty.",
                nameof(command));
        }

        if (!_handlers.TryAdd(command.CommandId, handler))
        {
            throw new InvalidOperationException(
                $"Command already registered: {command.CommandId}");
        }
    }

    public Task ExecuteAsync(
        string commandId,
        CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(commandId, out var handler))
        {
            throw new KeyNotFoundException(
                $"Command not registered: {commandId}");
        }

        return handler(cancellationToken);
    }
}

public sealed class EnvironmentPathResolver
{
    public string Resolve(
        string path,
        bool createDirectory = false)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "Path must not be empty.",
                nameof(path));
        }

        var expanded = Environment.ExpandEnvironmentVariables(path);
        var fullPath = Path.GetFullPath(expanded);

        if (createDirectory)
        {
            Directory.CreateDirectory(fullPath);
        }

        return fullPath;
    }
}

public sealed class JsonConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<T> LoadOrCreateAsync<T>(
        string path,
        T defaultValue,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var parent = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        if (!File.Exists(path))
        {
            await SaveAsync(path, defaultValue, cancellationToken);
            return defaultValue;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);

        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                $"Unable to deserialize configuration: {path}");
    }

    public async Task SaveAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var parent = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var json = JsonSerializer.Serialize(value, JsonOptions);
        var temporaryPath = $"{path}.tmp";

        await File.WriteAllTextAsync(
            temporaryPath,
            json,
            cancellationToken);

        File.Move(
            temporaryPath,
            path,
            overwrite: true);
    }
}

public sealed class JsonWidgetStateStore : IWidgetStateStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonConfigurationLoader _loader = new();

    public JsonWidgetStateStore(string path)
    {
        _path = path;
    }

    public async Task<T?> ReadAsync<T>(
        string key,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await _loader.LoadOrCreateAsync(
                _path,
                new Dictionary<string, JsonElement>(
                    StringComparer.OrdinalIgnoreCase),
                cancellationToken);

            return state.TryGetValue(key, out var value)
                ? value.Deserialize<T>()
                : default;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteAsync<T>(
        string key,
        T value,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await _loader.LoadOrCreateAsync(
                _path,
                new Dictionary<string, JsonElement>(
                    StringComparer.OrdinalIgnoreCase),
                cancellationToken);

            state[key] = JsonSerializer.SerializeToElement(value);

            await _loader.SaveAsync(
                _path,
                state,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}

public sealed class JsonWidgetSettingsStore : IWidgetSettingsStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonConfigurationLoader _loader = new();

    public JsonWidgetSettingsStore(string path)
    {
        _path = path;
    }

    public async Task<T> GetAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var settings = await _loader.LoadOrCreateAsync(
                _path,
                new Dictionary<string, JsonElement>(
                    StringComparer.OrdinalIgnoreCase),
                cancellationToken);

            return settings.TryGetValue(key, out var value)
                ? value.Deserialize<T>() ?? defaultValue
                : defaultValue;
        }
        finally
        {
            _gate.Release();
        }
    }
}

public sealed class JsonLocalizationService : ILocalizationService
{
    private readonly string _resourceDirectory;
    private readonly ConcurrentDictionary<
        string,
        IReadOnlyDictionary<string, string>> _cache =
            new(StringComparer.OrdinalIgnoreCase);

    public JsonLocalizationService(
        string resourceDirectory,
        string defaultCultureName = "en")
    {
        _resourceDirectory = resourceDirectory;
        CurrentCultureName = defaultCultureName;
    }

    public string CurrentCultureName { get; private set; }

    public string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var current = Load(CurrentCultureName);

        if (current.TryGetValue(key, out var value))
        {
            return value;
        }

        var fallback = Load("en");

        return fallback.TryGetValue(key, out value)
            ? value
            : key;
    }

    public string Get(
        string key,
        params object[] arguments)
    {
        return string.Format(
            CultureInfo.GetCultureInfo(CurrentCultureName),
            Get(key),
            arguments);
    }

    public Task SetCultureAsync(
        string cultureName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CultureInfo.GetCultureInfo(cultureName);
        CurrentCultureName = cultureName;

        return Task.CompletedTask;
    }

    private IReadOnlyDictionary<string, string> Load(
        string cultureName)
    {
        return _cache.GetOrAdd(
            cultureName,
            LoadFromDisk);
    }

    private IReadOnlyDictionary<string, string> LoadFromDisk(
        string cultureName)
    {
        var path = Path.Combine(
            _resourceDirectory,
            $"strings.{cultureName}.json");

        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record CoreRuntimeServices(
    IClock Clock,
    IWidgetLogger Logger,
    IEventBus EventBus,
    ICommandRegistry Commands,
    IWidgetStateStore StateStore,
    IWidgetSettingsStore SettingsStore,
    ILocalizationService Localization);

public static class CoreRuntimeFactory
{
    public static CoreRuntimeServices Create(
        string dataRoot,
        string resourceDirectory,
        string settingsFile)
    {
        var resolver = new EnvironmentPathResolver();
        var resolvedRoot = resolver.Resolve(
            dataRoot,
            createDirectory: true);

        return new CoreRuntimeServices(
            new SystemClock(),
            new ConsoleWidgetLogger(),
            new InMemoryEventBus(),
            new CommandRegistry(),
            new JsonWidgetStateStore(
                Path.Combine(
                    resolvedRoot,
                    "state",
                    "widget-state.json")),
            new JsonWidgetSettingsStore(settingsFile),
            new JsonLocalizationService(resourceDirectory));
    }
}