using System.IO;
using KRDesktopHub.Platform.Windows;

var temporaryRoot =
    Path.Combine(
        Path.GetTempPath(),
        "KRDesktopHub",
        "window-placement-smoke",
        Guid.NewGuid().ToString("N"));

Directory.CreateDirectory(
    temporaryRoot);

try
{
    var stateFile =
        Path.Combine(
            temporaryRoot,
            "state",
            "window-placement.json");

    var store =
        new JsonWindowPlacementStore(
            stateFile);

    if (store.TryLoad() is not null)
    {
        throw new InvalidOperationException(
            "First launch without state must return null.");
    }

    var monitors =
        new[]
        {
            new MonitorWorkingArea(
                "primary",
                0,
                0,
                1920,
                1040,
                IsPrimary:
                    true),

            new MonitorWorkingArea(
                "secondary",
                1920,
                0,
                1280,
                1024,
                IsPrimary:
                    false)
        };

    var defaults =
        WindowPlacementDefaults.Recommended;

    if (
        defaults.DefaultWidth != 600
        || defaults.MinimumWidth != 600
        || defaults.MinimumHeight != 240
    )
    {
        throw new InvalidOperationException(
            "CoreHost popup geometry contract must remain fixed at 600-DIP minimum width and 240-DIP minimum height.");
    }

    var normalized =
        WindowPlacementPolicy.Normalize(
            new WindowPlacementState(
                SchemaVersion:
                    1,

                PanelId:
                    "main-panel",

                Left:
                    2100,

                Top:
                    100,

                Width:
                    500,

                Height:
                    800,

                WindowState:
                    "Normal",

                MonitorDeviceName:
                    "secondary",

                SavedAtUtc:
                    DateTimeOffset.UtcNow),

            monitors,
            defaults);

    store.Save(
        normalized);

    var serializedState =
        await File.ReadAllTextAsync(
            stateFile);

    if (
        !serializedState.Contains(
            "\"schema_version\"",
            StringComparison.Ordinal)
        || !serializedState.Contains(
            "\"panel_id\"",
            StringComparison.Ordinal)
    )
    {
        throw new InvalidOperationException(
            "Window-placement JSON must use snake_case property names.");
    }

    var loaded =
        store.TryLoad()
        ?? throw new InvalidOperationException(
            "Saved placement could not be loaded.");

    if (
        loaded.Left != normalized.Left
        || loaded.Top != normalized.Top
        || loaded.Width != normalized.Width
        || loaded.Height != normalized.Height
    )
    {
        throw new InvalidOperationException(
            "Saved placement was not restored accurately.");
    }

    await File.WriteAllTextAsync(
        stateFile,
        "{ invalid json");

    if (store.TryLoad() is not null)
    {
        throw new InvalidOperationException(
            "Corrupt JSON must safely fall back to null.");
    }

    var incompatibleSchema =
        WindowPlacementPolicy.Normalize(
            new WindowPlacementState(
                SchemaVersion:
                    999,

                PanelId:
                    "main-panel",

                Left:
                    700,

                Top:
                    700,

                Width:
                    700,

                Height:
                    700,

                WindowState:
                    "Normal",

                MonitorDeviceName:
                    "secondary",

                SavedAtUtc:
                    DateTimeOffset.UtcNow),

            monitors,
            defaults);

    if (
        incompatibleSchema.Width != defaults.DefaultWidth
        || incompatibleSchema.Height != defaults.DefaultHeight
    )
    {
        throw new InvalidOperationException(
            "Incompatible schema must safely fall back to defaults.");
    }

    var offscreen =
        WindowPlacementPolicy.Normalize(
            new WindowPlacementState(
                SchemaVersion:
                    1,

                PanelId:
                    "main-panel",

                Left:
                    9000,

                Top:
                    9000,

                Width:
                    5000,

                Height:
                    5000,

                WindowState:
                    "Minimized",

                MonitorDeviceName:
                    "missing-monitor",

                SavedAtUtc:
                    DateTimeOffset.UtcNow),

            monitors,
            defaults);

    if (
        offscreen.Left < 0
        || offscreen.Top < 0
        || offscreen.Left + offscreen.Width > 1920
        || offscreen.Top + offscreen.Height > 1040
    )
    {
        throw new InvalidOperationException(
            "Off-screen placement was not clamped into the primary working area.");
    }

    if (offscreen.WindowState != "Normal")
    {
        throw new InvalidOperationException(
            "Minimized placement must restore as Normal.");
    }

    var undersized =
        WindowPlacementPolicy.Normalize(
            new WindowPlacementState(
                SchemaVersion:
                    1,

                PanelId:
                    "main-panel",

                Left:
                    0,

                Top:
                    0,

                Width:
                    1,

                Height:
                    1,

                WindowState:
                    "Normal",

                MonitorDeviceName:
                    "primary",

                SavedAtUtc:
                    DateTimeOffset.UtcNow),

            monitors,
            defaults);

    if (
        undersized.Width != defaults.MinimumWidth
        || undersized.Height != defaults.MinimumHeight
    )
    {
        throw new InvalidOperationException(
            "Minimum size clamp failed.");
    }

    var previousOverride =
        Environment.GetEnvironmentVariable(
            CoreHostDataRootResolver.OverrideEnvironmentVariable);

    try
    {
        Environment.SetEnvironmentVariable(
            CoreHostDataRootResolver.OverrideEnvironmentVariable,
            temporaryRoot);

        if (
            CoreHostDataRootResolver.ResolveDefaultDataRoot()
            != Path.GetFullPath(
                temporaryRoot)
        )
        {
            throw new InvalidOperationException(
                "Configurable data-root override failed.");
        }
    }
    finally
    {
        Environment.SetEnvironmentVariable(
            CoreHostDataRootResolver.OverrideEnvironmentVariable,
            previousOverride);
    }

    Console.WriteLine(
        "Batch 8A Window Placement smoke test passed.");
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