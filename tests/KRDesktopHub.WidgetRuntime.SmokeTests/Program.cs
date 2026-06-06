using System.Text.Json;
using KRDesktopHub.Contracts;
using KRDesktopHub.Core;

var repositoryRoot =
    Environment.CurrentDirectory;

var temporaryRoot =
    Path.Combine(
        Path.GetTempPath(),
        "KRDesktopHub",
        "batch4-smoke",
        Guid.NewGuid().ToString("N"));

try
{
    var resources =
        Path.Combine(
            temporaryRoot,
            "resources");

    var settings =
        Path.Combine(
            temporaryRoot,
            "config",
            "widget-settings.json");

    Directory.CreateDirectory(resources);

    await File.WriteAllTextAsync(
        Path.Combine(
            resources,
            "strings.en.json"),

        """
        {
          "widget.ready": "Ready"
        }
        """);

    var services =
        CoreRuntimeFactory.Create(
            temporaryRoot,
            resources,
            settings);

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

    var fixtureDirectory =
        Path.Combine(
            repositoryRoot,
            "widgets",
            "fixtures",
            "KRDesktopHub.Fixture.Basic");

    using var loaded =
        await loader.LoadAsync(
            fixtureDirectory,
            CancellationToken.None);

    var installedFixtureDirectory =
        Path.Combine(
            temporaryRoot,
            "plugins",
            "installed",
            "kr.fixture.basic.package");

    var installedFixtureLibrary =
        Path.Combine(
            installedFixtureDirectory,
            "lib");

    Directory.CreateDirectory(
        installedFixtureLibrary);

    File.Copy(
        Path.Combine(
            fixtureDirectory,
            "bin",
            "Release",
            "net10.0",
            "KRDesktopHub.Fixture.Basic.dll"),
        Path.Combine(
            installedFixtureLibrary,
            "KRDesktopHub.Fixture.Basic.dll"));

    await File.WriteAllTextAsync(
        Path.Combine(
            installedFixtureDirectory,
            "manifest.json"),
        JsonSerializer.Serialize(
            new WidgetPackageManifest
            {
                ManifestSchemaVersion =
                    1,

                WidgetId =
                    "kr.fixture.basic",

                DisplayName =
                    "Basic Fixture Widget",

                PackageVersion =
                    "0.1.0",

                RequiredContractsVersion =
                    "1.0.0",

                MinimumHostVersion =
                    "0.1.0",

                EntryAssembly =
                    "lib/KRDesktopHub.Fixture.Basic.dll",

                EntryType =
                    "KRDesktopHub.Fixture.Basic.BasicFixtureWidget",

                ActivationMode =
                    WidgetActivationMode.OnDemand,

                Capabilities =
                    new[]
                    {
                        "lifecycle",
                        "state_store"
                    }
            }));

    await ValidateInstalledPackageManifestAdaptationAsync(
        loader,
        installedFixtureDirectory);

    await using var controller =
        new WidgetRuntimeController(
            new WidgetRuntimePolicy(
                MaxRetries:
                    1,

                QuarantineAfterFailedCycles:
                    2,

                OperationTimeout:
                    TimeSpan.FromSeconds(2),

                MaxConcurrentOperations:
                    2));

    controller.Register(
        loaded.Widget);

    await controller.InitializeAsync(
        loaded.Manifest.WidgetId,
        context,
        CancellationToken.None);

    await controller.StartAsync(
        loaded.Manifest.WidgetId,
        CancellationToken.None);

    await controller.PauseAsync(
        loaded.Manifest.WidgetId,
        CancellationToken.None);

    await controller.ResumeAsync(
        loaded.Manifest.WidgetId,
        CancellationToken.None);

    await controller.StopAsync(
        loaded.Manifest.WidgetId,
        CancellationToken.None);

    var lifecycle =
        await services.StateStore.ReadAsync<string>(
            "fixture.lifecycle",
            CancellationToken.None);

    if (lifecycle != "stopped")
    {
        throw new InvalidOperationException(
            "Fixture Widget lifecycle validation failed.");
    }

    var snapshot =
        controller.GetSnapshot(
            loaded.Manifest.WidgetId);

    if (snapshot.State !=
        WidgetRuntimeState.Disabled)
    {
        throw new InvalidOperationException(
            "Widget Runtime state validation failed.");
    }

    var scheduledRuns =
        0;

    await scheduler.ScheduleAsync(
        "smoke.periodic",
        TimeSpan.FromMilliseconds(20),
        _ =>
        {
            Interlocked.Increment(
                ref scheduledRuns);

            return Task.CompletedTask;
        },
        CancellationToken.None);

    await Task.Delay(120);

    await scheduler.CancelAsync(
        "smoke.periodic",
        CancellationToken.None);

    if (scheduledRuns < 2)
    {
        throw new InvalidOperationException(
            "Periodic scheduler validation failed.");
    }

    await using var failingController =
        new WidgetRuntimeController(
            new WidgetRuntimePolicy(
                MaxRetries:
                    1,

                QuarantineAfterFailedCycles:
                    2,

                OperationTimeout:
                    TimeSpan.FromMilliseconds(100),

                MaxConcurrentOperations:
                    1));

    var failingWidget =
        new FailingWidget();

    failingController.Register(
        failingWidget);

    for (var cycle = 0;
        cycle < 2;
        cycle++)
    {
        try
        {
            await failingController.StartAsync(
                failingWidget.Descriptor.WidgetId,
                CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
        }
    }

    var failingSnapshot =
        failingController.GetSnapshot(
            failingWidget.Descriptor.WidgetId);

    if (failingSnapshot.State !=
        WidgetRuntimeState.Quarantined)
    {
        throw new InvalidOperationException(
            "Widget quarantine validation failed.");
    }

    if (failingWidget.Attempts != 4)
    {
        throw new InvalidOperationException(
            "Widget retry validation failed.");
    }

}
finally
{
    await DeleteTemporaryDirectoryWithAssemblyUnloadRetryAsync(
        temporaryRoot);
}

Console.WriteLine(
    "Batch 4 Widget Runtime smoke test passed.");

static async Task ValidateInstalledPackageManifestAdaptationAsync(
    WidgetPluginLoader loader,
    string installedFixtureDirectory)
{
    using var installedLoaded =
        await loader.LoadAsync(
            installedFixtureDirectory,
            CancellationToken.None);

    if (
        installedLoaded.Manifest.WidgetId
            != "kr.fixture.basic"
        || installedLoaded.Manifest.DisplayName
            != "Basic Fixture Widget"
        || installedLoaded.Manifest.EntryAssembly
            != "lib/KRDesktopHub.Fixture.Basic.dll"
        || installedLoaded.Manifest.ActivationMode
            != WidgetActivationMode.OnDemand
    )
    {
        throw new InvalidOperationException(
            "Installed package-to-runtime manifest adaptation validation failed.");
    }
}

static async Task DeleteTemporaryDirectoryWithAssemblyUnloadRetryAsync(
    string temporaryRoot)
{
    const int maximumAttempts =
        8;

    for (var attempt = 1;
        attempt <= maximumAttempts;
        attempt++)
    {
        if (!Directory.Exists(
            temporaryRoot))
        {
            return;
        }

        try
        {
            Directory.Delete(
                temporaryRoot,
                recursive: true);

            return;
        }
        catch (Exception exception)
            when (
                exception is UnauthorizedAccessException
                || exception is IOException)
        {
            if (attempt == maximumAttempts)
            {
                throw new InvalidOperationException(
                    "Widget Runtime smoke-test temporary cleanup exhausted retry budget.",
                    exception);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            await Task.Delay(
                TimeSpan.FromMilliseconds(
                    125 * attempt));
        }
    }
}

public sealed class FailingWidget
    : IKrWidget
{
    public int Attempts { get; private set; }

    public WidgetDescriptor Descriptor { get; } =
        new(
            "kr.fixture.failure",
            "Failure Fixture Widget",
            new Version(0, 1, 0),
            new Version(1, 0, 0),
            new Version(0, 1, 0),
            new[]
            {
                "failure"
            });

    public Task InitializeAsync(
        IWidgetContext context,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        Attempts++;

        throw new InvalidOperationException(
            "Expected fixture failure.");
    }

    public Task PauseAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ResumeAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
