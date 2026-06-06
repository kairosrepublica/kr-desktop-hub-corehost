using KRDesktopHub.App.Windows;
using System;
using System.IO;
using KRDesktopHub.Platform.Windows;

var tempRoot =
    Path.Combine(
        Path.GetTempPath(),
        "KRDesktopHub-SettingsCenterRuntimeBridge-"
        + Guid
            .NewGuid()
            .ToString(
                "N"));

Directory.CreateDirectory(
    tempRoot);

try
{
    var bridge =
        new SettingsCenterRuntimeBridge(
            tempRoot);

    var document =
        bridge.LoadOrCreate();

    document.Settings.ShowHidePanelHotkey =
        "Ctrl+Alt+H";

    document.Settings.PanelAlwaysOnTop =
        true;

    document.Settings.StartupDelaySeconds =
        17;

    bridge.Save(
        document);

    var runtimeStore =
        new JsonCoreHostSettingsStore(
            tempRoot);

    var runtime =
        runtimeStore.Reload();

    if (
        runtime.TogglePanelHotkey
            != "Ctrl+Alt+H"
        || !runtime.AlwaysOnTop
        || runtime.StartupDelaySeconds
            != 17
    )
    {
        throw new InvalidOperationException(
            "Settings Center UI-to-runtime bridge validation failed.");
    }

    runtimeStore.Save(
        runtime with
        {
            TogglePanelHotkey =
                "Ctrl+Shift+J",

            SavedAtUtc =
                DateTimeOffset.UtcNow
        });

    var reflectedDocument =
        bridge.LoadOrCreate();

    if (
        reflectedDocument
            .Settings
            .ShowHidePanelHotkey
        != "Ctrl+Shift+J"
    )
    {
        throw new InvalidOperationException(
            "Active runtime settings-to-UI bridge validation failed.");
    }

    var quietHoursDocument =
        bridge.LoadOrCreate();

    quietHoursDocument.Settings.QuietHoursStart =
        "";

    quietHoursDocument.Settings.QuietHoursEnd =
        "";

    bridge.Save(
        quietHoursDocument);

    var quietHoursRuntime =
        runtimeStore.Reload();

    var quietHoursReloadedDocument =
        bridge.LoadOrCreate();

    if (
        quietHoursRuntime.QuietHoursEnabled
        || quietHoursReloadedDocument.Settings.QuietHoursEnabled
        || quietHoursReloadedDocument.Settings.QuietHoursStart != ""
        || quietHoursReloadedDocument.Settings.QuietHoursEnd != ""
    )
    {
        throw new InvalidOperationException(
            "Cleared quiet-hours values reappeared after save and reload.");
    }

    quietHoursReloadedDocument.Settings.QuietHoursEnabled =
        true;

    quietHoursReloadedDocument.Settings.QuietHoursStart =
        "21:30";

    quietHoursReloadedDocument.Settings.QuietHoursEnd =
        "06:45";

    bridge.Save(
        quietHoursReloadedDocument);

    var restoredQuietHours =
        bridge.LoadOrCreate();

    if (
        !restoredQuietHours.Settings.QuietHoursEnabled
        || restoredQuietHours.Settings.QuietHoursStart != "21:30"
        || restoredQuietHours.Settings.QuietHoursEnd != "06:45"
    )
    {
        throw new InvalidOperationException(
            "Explicit quiet-hours re-enable validation failed.");
    }

    var centerPath =
        Path.Combine(
            tempRoot,
            "config",
            "corehost-settings-center.json");

    var runtimePath =
        Path.Combine(
            tempRoot,
            "config",
            "corehost-settings.json");

    if (
        !File.Exists(
            centerPath)
        || !File.Exists(
            runtimePath)
    )
    {
        throw new InvalidOperationException(
            "Settings Center bridge persistence files were not created.");
    }

    Console.WriteLine(
        "Batch 8D2 Settings Center runtime bridge smoke test passed.");
}
finally
{
    if (Directory.Exists(
        tempRoot))
    {
        Directory.Delete(
            tempRoot,
            recursive:
                true);
    }
}
