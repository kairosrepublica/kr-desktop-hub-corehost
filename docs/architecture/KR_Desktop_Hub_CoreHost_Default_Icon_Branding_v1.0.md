# KR Desktop Hub CoreHost Default Icon Branding v1.0

## Purpose

Freeze the default branded KR icon as a CoreHost-owned platform asset.

## Assets

```text
src/KRDesktopHub.App.Windows/Assets/CoreHost/KRDesktopHub.CoreHost.ico
src/KRDesktopHub.App.Windows/Assets/CoreHost/KRDesktopHub.CoreHost.png
src/KRDesktopHub.App.Windows/Assets/CoreHost/KRDesktopHub.CoreHost.svg
```

## Usage

```text
EXE application icon:
KRDesktopHub.App.Windows.csproj
ApplicationIcon

WPF popup-window icon:
MainWindow.xaml
Icon

Tray default visual state:
WindowsTrayVisualStateCatalog.Default
CoreHostDefaultIconCatalog.Resolve()
```

## Windows ICO contents

The generated `.ico` contains these raster sizes:

```text
16
20
24
32
40
48
64
128
256
```

## Ownership boundary

```text
CoreHost owns:
default executable icon
default popup-window icon
tray icon broker
approved tray visual-state registry
fallback icon

Production Widgets may request:
approved declarative tray visual-state keys only

Production Widgets must not:
own the tray icon
submit arbitrary icon file paths
replace CoreHost branded assets
```

## Resilience

If executable-associated icon extraction fails, the tray default state falls back to:

```text
SystemIcons.Application
```
