using System.IO;
using KRDesktopHub.Platform.Windows;

var temporaryRoot =
    Path.Combine(
        Path.GetTempPath(),
        "KRDesktopHub",
        "corehost-settings-smoke",
        Guid.NewGuid().ToString("N"));

Directory.CreateDirectory(
    temporaryRoot);

try
{
    var store =
        new JsonCoreHostSettingsStore(
            temporaryRoot);

    var defaults =
        store.LoadOrCreateRecommended();

    if (
        !defaults.LoginStartupEnabled
        || defaults.StartupDelaySeconds != 10
        || !defaults.PanelHiddenAfterLogin
        || !defaults.CloseButtonHidesToTray
        || defaults.TogglePanelHotkey != "Ctrl+Alt+K"
    )
    {
        throw new InvalidOperationException(
            "Recommended CoreHost defaults are incorrect.");
    }

    var serializedDefaults =
        await File.ReadAllTextAsync(
            store.SettingsFilePath);

    if (
        !serializedDefaults.Contains(
            "\"login_startup_enabled\"",
            StringComparison.Ordinal)
        || !serializedDefaults.Contains(
            "\"toggle_panel_hotkey\"",
            StringComparison.Ordinal)
    )
    {
        throw new InvalidOperationException(
            "CoreHost settings JSON must use snake_case property names.");
    }

    var invalid =
        defaults with
        {
            StartupDelaySeconds =
                9999,

            TogglePanelHotkey =
                "Ctrl+Alt+K",

            TogglePanelHotkeyFallbacks =
                new[]
                {
                    "Ctrl+Alt+K",
                    "Ctrl+Alt+H",
                    "Ctrl+Alt+H",
                    "Win+Alt+K"
                },

            WidgetRetryCount =
                999,

            WidgetTaskTimeoutSeconds =
                -1,

            QuietHoursStartLocal =
                "invalid"
        };

    store.Save(
        invalid);

    var normalized =
        store.Reload();

    if (
        normalized.StartupDelaySeconds != 300
        || normalized.WidgetRetryCount != 20
        || normalized.WidgetTaskTimeoutSeconds != 1
        || normalized.QuietHoursStartLocal != "23:00"
    )
    {
        throw new InvalidOperationException(
            "Settings clamp or fallback validation failed.");
    }

    if (
        normalized
            .TogglePanelHotkeyFallbacks
            .Count(
                gesture =>
                    string.Equals(
                        gesture,
                        "Ctrl+Alt+H",
                        StringComparison.OrdinalIgnoreCase))
        != 1
    )
    {
        throw new InvalidOperationException(
            "Hotkey fallback de-duplication failed.");
    }

    var candidates =
        CoreHostHotkeyPolicy.GetCandidateGestures(
            normalized);

    if (
        candidates.Count < 2
        || candidates[0] != "Ctrl+Alt+K"
        || candidates.Distinct(
            StringComparer.OrdinalIgnoreCase)
            .Count() != candidates.Count
    )
    {
        throw new InvalidOperationException(
            "Hotkey candidate ordering or uniqueness failed.");
    }

    var incompatibleSchema =
        normalized with
        {
            SchemaVersion =
                999,

            LoginStartupEnabled =
                false
        };

    await File.WriteAllTextAsync(
        store.SettingsFilePath,
        System.Text.Json.JsonSerializer.Serialize(
            incompatibleSchema,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.SnakeCaseLower,

                WriteIndented =
                    true
            }));

    var reset =
        store.Reload();

    if (!reset.LoginStartupEnabled)
    {
        throw new InvalidOperationException(
            "Incompatible settings schema must fall back to recommended defaults.");
    }

    var recommendationNames =
        CoreHostSettingsCatalog
            .Recommendations
            .Select(
                recommendation =>
                    recommendation.SettingName)
            .ToHashSet(
                StringComparer.Ordinal);

    var editableSettingNames =
        typeof(
            CoreHostSettings)
            .GetProperties()
            .Select(
                property =>
                    property.Name)
            .Where(
                name =>
                    name !=
                        nameof(
                            CoreHostSettings.SchemaVersion)
                    && name !=
                        nameof(
                            CoreHostSettings.SavedAtUtc))
            .ToHashSet(
                StringComparer.Ordinal);

    if (
        !editableSettingNames.SetEquals(
            recommendationNames)
        || CoreHostSettingsCatalog
            .Recommendations
            .Any(
                recommendation =>
                    string.IsNullOrWhiteSpace(
                        recommendation.Reason))
    )
    {
        throw new InvalidOperationException(
            "Recommended-default reasons must cover every editable setting.");
    }

    var runtimeStore =
        new JsonHotkeyRegistrationRuntimeStateStore(
            temporaryRoot);

    runtimeStore.Save(
        new HotkeyRegistrationRuntimeState(
            SchemaVersion:
                1,

            CommandId:
                "panel.toggle",

            RequestedGesture:
                "Ctrl+Alt+K",

            ActiveGesture:
                "Ctrl+Alt+H",

            Registered:
                true,

            AttemptedGestures:
                new[]
                {
                    "Ctrl+Alt+K",
                    "Ctrl+Alt+H"
                },

            LastError:
                null,

            SavedAtUtc:
                DateTimeOffset.UtcNow));

    var runtimeJson =
        await File.ReadAllTextAsync(
            runtimeStore.StateFilePath);

    if (
        !runtimeJson.Contains(
            "\"active_gesture\"",
            StringComparison.Ordinal)
    )
    {
        throw new InvalidOperationException(
            "Hotkey runtime diagnostic JSON must use snake_case property names.");
    }

    var runtimeState =
        System.Text.Json.JsonSerializer.Deserialize<HotkeyRegistrationRuntimeState>(
            runtimeJson,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.SnakeCaseLower,

                PropertyNameCaseInsensitive =
                    true
            })
        ?? throw new InvalidOperationException(
            "Hotkey runtime diagnostic state could not be deserialized.");

    if (
        !runtimeState.Registered
        || runtimeState.ActiveGesture != "Ctrl+Alt+H"
        || runtimeState.AttemptedGestures.Count != 2
        || runtimeState.AttemptedGestures[0] != "Ctrl+Alt+K"
        || runtimeState.AttemptedGestures[1] != "Ctrl+Alt+H"
    )
    {
        throw new InvalidOperationException(
            "Hotkey runtime diagnostic semantic state was not persisted.");
    }

    var parsed =
        WindowsGlobalHotkeyService.ParseGesture(
            "Ctrl+Alt+K");

    if (
        parsed.Modifiers == 0
        || parsed.VirtualKey == 0
    )
    {
        throw new InvalidOperationException(
            "Valid hotkey parsing failed.");
    }

    var invalidHotkeyRejected =
        false;

    try
    {
        _ =
            WindowsGlobalHotkeyService.ParseGesture(
                "K");
    }
    catch (FormatException)
    {
        invalidHotkeyRejected =
            true;
    }

    if (!invalidHotkeyRejected)
    {
        throw new InvalidOperationException(
            "Invalid hotkey must be rejected.");
    }

    Console.WriteLine(
        "Batch 8B1 CoreHost Settings smoke test passed.");
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