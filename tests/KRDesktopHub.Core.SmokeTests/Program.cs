using KRDesktopHub.Contracts;
using KRDesktopHub.Core;

var temporaryRoot = Path.Combine(
    Path.GetTempPath(),
    "KRDesktopHub",
    "batch2-smoke",
    Guid.NewGuid().ToString("N"));

try
{
    var resources = Path.Combine(
        temporaryRoot,
        "resources");

    var config = Path.Combine(
        temporaryRoot,
        "config",
        "widget-settings.json");

    Directory.CreateDirectory(resources);
    Directory.CreateDirectory(
        Path.GetDirectoryName(config)!);

    await File.WriteAllTextAsync(
        Path.Combine(resources, "strings.en.json"),
        """
        {
          "greeting": "Hello {0}",
          "fallback_only": "Fallback works"
        }
        """);

    await File.WriteAllTextAsync(
        config,
        """
        {
          "refresh_seconds": 15
        }
        """);

    var runtime = CoreRuntimeFactory.Create(
        temporaryRoot,
        resources,
        config);

    if (Math.Abs(
        (runtime.Clock.UtcNow - DateTimeOffset.UtcNow)
            .TotalSeconds) > 2)
    {
        throw new InvalidOperationException(
            "Clock validation failed.");
    }

    await runtime.StateStore.WriteAsync(
        "counter",
        42,
        CancellationToken.None);

    var storedCounter =
        await runtime.StateStore.ReadAsync<int>(
            "counter",
            CancellationToken.None);

    if (storedCounter != 42)
    {
        throw new InvalidOperationException(
            "State-store validation failed.");
    }

    var refreshSeconds =
        await runtime.SettingsStore.GetAsync(
            "refresh_seconds",
            30,
            CancellationToken.None);

    if (refreshSeconds != 15)
    {
        throw new InvalidOperationException(
            "Settings-store validation failed.");
    }

    var observedEvent = 0;

    using var subscription =
        runtime.EventBus.Subscribe<int>(
            (value, _) =>
            {
                observedEvent = value;
                return Task.CompletedTask;
            });

    await runtime.EventBus.PublishAsync(
        7,
        CancellationToken.None);

    if (observedEvent != 7)
    {
        throw new InvalidOperationException(
            "Event-bus validation failed.");
    }

    var commandExecuted = false;

    runtime.Commands.Register(
        new WidgetCommand(
            "smoke.execute",
            "Smoke Execute",
            "Validates command execution."),
        _ =>
        {
            commandExecuted = true;
            return Task.CompletedTask;
        });

    await runtime.Commands.ExecuteAsync(
        "smoke.execute",
        CancellationToken.None);

    if (!commandExecuted)
    {
        throw new InvalidOperationException(
            "Command-registry validation failed.");
    }

    if (runtime.Localization.Get(
        "greeting",
        "KR") != "Hello KR")
    {
        throw new InvalidOperationException(
            "Localization validation failed.");
    }

    await runtime.Localization.SetCultureAsync(
        "tr-TR",
        CancellationToken.None);

    if (runtime.Localization.Get(
        "fallback_only") != "Fallback works")
    {
        throw new InvalidOperationException(
            "Localization fallback validation failed.");
    }

    var resolvedPath =
        new EnvironmentPathResolver().Resolve(
            "%TEMP%",
            createDirectory: true);

    if (!Directory.Exists(resolvedPath))
    {
        throw new InvalidOperationException(
            "Path-resolver validation failed.");
    }

    Console.WriteLine(
        "Batch 2 Core Runtime smoke test passed.");
}
finally
{
    if (Directory.Exists(temporaryRoot))
    {
        Directory.Delete(
            temporaryRoot,
            recursive: true);
    }
}