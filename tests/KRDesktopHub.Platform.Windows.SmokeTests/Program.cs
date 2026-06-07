using KRDesktopHub.Platform.Abstractions;
using KRDesktopHub.Platform.Windows;

var command =
    StartupCommandBuilder.Build(
        @"C:\Apps\KR Desktop Hub\KRDesktopHub.App.Windows.exe",
        new StartupRegistration(
            Enabled: true,
            Delay: TimeSpan.FromSeconds(10)));

if (command !=
    "\"C:\\Apps\\KR Desktop Hub\\KRDesktopHub.App.Windows.exe\" --start-hidden --startup-delay-seconds 10")
{
    throw new InvalidOperationException(
        "Startup command validation failed.");
}

if (StartupCommandBuilder.ParseDelay(command) !=
    TimeSpan.FromSeconds(10))
{
    throw new InvalidOperationException(
        "Startup delay parsing failed.");
}

var hotkey =
    WindowsGlobalHotkeyService.ParseGesture(
        "Ctrl+Alt+K");

if (hotkey.Modifiers == 0 ||
    hotkey.VirtualKey == 0)
{
    throw new InvalidOperationException(
        "Hotkey parsing failed.");
}

var platform =
    new WindowsPlatformInfoService();

if (string.IsNullOrWhiteSpace(
    platform.OperatingSystem))
{
    throw new InvalidOperationException(
        "Platform information validation failed.");
}

if (CoreHostTrayStatusText.Ready != "KR Desktop Hub - Ready")
{
    throw new InvalidOperationException(
        "Tray tooltip exact-text regression validation failed.");
}

if (
    WindowsTrayVisualStateCatalog.ResolveIcon(
        WindowsTrayVisualStateCatalog.Warning)
    != System.Drawing.SystemIcons.Warning
    || WindowsTrayVisualStateCatalog.ResolveIcon(
        "unknown.visual-state")
        != System.Drawing.SystemIcons.Application
)
{
    throw new InvalidOperationException(
        "Approved tray visual-state registry validation failed.");
}

var defaultTrayIcon =
    WindowsTrayVisualStateCatalog.ResolveIcon(
        WindowsTrayVisualStateCatalog.Default);

if (defaultTrayIcon.Width <= 0
    || defaultTrayIcon.Height <= 0)
{
    throw new InvalidOperationException(
        "CoreHost default tray icon resolution validation failed.");
}

foreach (var character in CoreHostTrayStatusText.Ready)
{
    if (character > 127)
    {
        throw new InvalidOperationException(
            "Tray tooltip ASCII regression validation failed.");
    }
}
Console.WriteLine(
    "Batch 3 Windows platform smoke test passed.");
