using System;
using System.IO;
using System.Linq;
using KRDesktopHub.Core;

var tempRoot =
    Path.Combine(
        Path.GetTempPath(),
        "KRDesktopHub-SettingsCenter-"
        + Guid
            .NewGuid()
            .ToString(
                "N"));

Directory.CreateDirectory(
    tempRoot);

try
{
    var service =
        new CoreHostSettingsCenterService(
            tempRoot);

    var created =
        service.LoadOrCreate();

    if (
        !created.Settings.StartAfterWindowsLogin
        || created.Settings.StartupDelaySeconds
            != 10
        || created.Settings.OpenPanelAfterLogin
        || !created.Settings.CloseButtonHidesToTray
        || !created.Settings.RememberWindowPlacement
        || created.Settings.ShowHidePanelHotkey
            != "Ctrl+Alt+K"
        || created.Settings.NotificationRateLimitPerMinute
            != 6
        || created.Settings.MaximumConcurrentWidgetTasks
            != 4
    )
    {
        throw new InvalidOperationException(
            "Settings Center recommended-default validation failed.");
    }

    if (
        CoreHostSettingsCenterCatalog
            .All
            .Count
        < 40
    )
    {
        throw new InvalidOperationException(
            "Settings Center descriptor coverage is incomplete.");
    }

    var duplicateKeys =
        CoreHostSettingsCenterCatalog
            .All
            .GroupBy(
                descriptor =>
                    descriptor.Key,
                StringComparer.Ordinal)
            .Where(
                group =>
                    group.Count()
                    > 1)
            .ToArray();

    if (duplicateKeys.Length
        > 0)
    {
        throw new InvalidOperationException(
            "Settings Center descriptor keys are not unique.");
    }

    var knownSections =
        CoreHostSettingsCenterCatalog
            .All
            .Select(
                descriptor =>
                    descriptor.SectionId)
            .Distinct(
                StringComparer.Ordinal)
            .ToHashSet(
                StringComparer.Ordinal);

    foreach (var requiredSection in
        new[]
        {
            "startup",
            "panel-tray",
            "hotkeys",
            "notifications",
            "runtime-resources",
            "diagnostics-migration"
        })
    {
        if (!knownSections.Contains(
            requiredSection))
        {
            throw new InvalidOperationException(
                $"Missing Settings Center section: {requiredSection}");
        }
    }

    created.Settings.StartupDelaySeconds =
        17;

    created.Settings.PanelAlwaysOnTop =
        true;

    created.Settings.QuietHoursStart =
        "22:00";

    created.Settings.QuietHoursEnd =
        "07:00";

    created.Settings.LogRetentionDays =
        21;

    service.Save(
        created);

    var reloaded =
        service.LoadOrCreate();

    if (
        reloaded.Settings.StartupDelaySeconds
            != 17
        || !reloaded.Settings.PanelAlwaysOnTop
        || reloaded.Settings.QuietHoursStart
            != "22:00"
        || reloaded.Settings.QuietHoursEnd
            != "07:00"
        || reloaded.Settings.LogRetentionDays
            != 21
    )
    {
        throw new InvalidOperationException(
            "Settings Center persistence validation failed.");
    }

    ExpectValidationFailure(
        () =>
        {
            reloaded.Settings.StartupDelaySeconds =
                301;

            service.Save(
                reloaded);
        },
        "Out-of-range startup-delay validation failed.");

    reloaded.Settings.StartupDelaySeconds =
        10;

    ExpectValidationFailure(
        () =>
        {
            reloaded.Settings.HidePanelHotkey =
                "Ctrl+Alt+K";

            service.Save(
                reloaded);
        },
        "Hotkey-collision validation failed.");

    reloaded.Settings.HidePanelHotkey =
        "";

    ExpectValidationFailure(
        () =>
        {
            reloaded.Settings.QuietHoursStart =
                "25:00";

            service.Save(
                reloaded);
        },
        "Quiet-hours time-format validation failed.");

    Console.WriteLine(
        "Batch 8D2 Settings Center smoke test passed.");
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

static void ExpectValidationFailure(
    Action action,
    string message)
{
    try
    {
        action();
    }
    catch (CoreHostSettingsValidationException)
    {
        return;
    }

    throw new InvalidOperationException(
        message);
}