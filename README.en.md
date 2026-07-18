# DarkReader

[![License: GPL-3.0](https://img.shields.io/badge/License-GPL--3.0-blue.svg)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-blue.svg)](#system-requirements)
[![Release](https://img.shields.io/github/v/release/CodingAQ/DarkReader-Desktop)](https://github.com/CodingAQ/DarkReader-Desktop/releases)

English | **[简体中文](./README.md)**

> **Acknowledgment**: This project is derived from [NegativeScreen](https://github.com/mlaily/NegativeScreen) (by [mlaily](https://github.com/mlaily), GPL-3.0).
> Key improvements: window-targeted inversion, region-limited inversion, tray menu refactor, and more.
> Licensed under [GPL-3.0](./LICENSE).

## Introduction

DarkReader is a Windows system tray application that instantly switches the entire screen to dark mode. It uses the built-in Windows Magnification API (compositor-level color matrix) with zero frame latency and negligible CPU usage.

## Screenshots

<!-- TODO: add screenshots
![Tray menu](docs/screenshot-tray.png)
![Dark mode effect](docs/screenshot-dark-mode.png)
![Region selection](docs/screenshot-region.png)
-->

📷 Screenshots to be added.

## Features

- **One-click** dark mode toggle, zero frame latency (compositor-level color matrix)
- **7 color modes**: Default + Preset 1-5 + Grayscale
- **Window inversion**: effect limited to a specific window, with auto-follow/pause
- **Region inversion**: effect limited to a screen rectangle
- **Global hotkeys** that work even when another app is in focus
- **Single-file publish**, no .NET Runtime installation required
- **Smooth transitions**: 150ms animation to avoid abrupt flicker
- **Persistent settings**, restored after restart
- **Single instance**: second launch toggles the switch

## Download & Install

1. Go to [Releases](https://github.com/CodingAQ/DarkReader-Desktop/releases) to download the latest package
2. Extract to any directory
3. Double-click `DarkReader.exe` to start
4. The app minimizes to the system tray (bottom-right notification area)

> **First run**: If prompted that Windows Aero/DWM is not enabled, click "OK" to continue (some systems may not require Aero).

## Usage

### Tray Icon

| Action | Effect |
|--------|--------|
| **Left-click** the tray icon | Toggle dark mode on/off |
| **Right-click** the tray icon | Open the mode menu |

### Menu Options

- **Toggle** — Turn dark mode on/off
- **Default** — Simple color inversion (classic negative effect)
- **Preset 1-5** — Five smart inversion modes (preserve hue, more comfortable)
- **Grayscale** — Global grayscale (luminance-based, keeps dark tones)
- **Select Region** — Choose an inversion region (current region shown in menu)
- **Clear Region** — Clear region restriction, restore fullscreen
- **Select Window** — Pick a target window from the list (auto-follow/resize/pause)
- **Clear Window Target** — Clear window target
- **Pause When Not Foreground** — Auto-pause when window loses focus (check to enable)
- **Active On Startup** — Auto-enable dark mode on launch (check to enable)
- **Exit** — Quit the application

### Global Hotkeys

| Hotkey | Function |
|--------|----------|
| `Win + Alt + N` | Toggle dark mode on/off |
| `Win + Alt + 1` | Switch to: Default |
| `Win + Alt + 2` | Switch to: Preset 1 |
| `Win + Alt + 3` | Switch to: Preset 2 |
| `Win + Alt + 4` | Switch to: Preset 3 |
| `Win + Alt + 5` | Switch to: Preset 4 |
| `Win + Alt + 6` | Switch to: Preset 5 |
| `Win + Alt + R` | Select inversion region |
| `Win + Alt + H` | Exit application |

> Grayscale mode is only accessible via the menu; it has no hotkey binding.

### Window Inversion

DarkReader can apply the inversion effect only to a specific window:

| Action | Effect |
|--------|--------|
| Menu → **Select Window** → pick a window | Choose a target window from the list |
| Menu → **Clear Window Target** | Clear the window target, restore fullscreen inversion |

**Smart behavior**:
- When the window moves, the inversion region **auto-follows**
- When the window resizes, the inversion region **auto-adjusts**
- When the window loses focus, the filter **auto-pauses** (resumes when refocused)
- When the window closes, the target is auto-cleared

> Toggle the "foreground pause" feature via the **Pause When Not Foreground** menu item.

### Region Inversion

DarkReader can restrict the inversion effect to a specific screen region:

| Action | Effect |
|--------|--------|
| Menu → **Select Region** | Enter region selection mode, drag to choose the area |
| Menu → **Clear Region** | Clear region restriction, restore fullscreen |
| `Win + Alt + R` | Hotkey to enter region selection mode |

**Region selection mode**:
- A semi-transparent overlay covers the screen
- **Left-drag** to select a rectangular area
- **Right-click** or **Esc** to cancel
- The selected area is highlighted

> Region settings are auto-saved and restored on restart.

## 7 Color Modes

| Mode | Description |
|------|-------------|
| **Default** | Simple RGB inversion, intense effect, suitable for high contrast needs |
| **Preset 1** | Theoretically optimal transform (Tom MacLeod's method), accurate colors but may oversaturate |
| **Preset 2** | Simplest 180° hue shift, high saturation, good for solid colors |
| **Preset 3** | Overall desaturated, yellows and blues darker, suitable for long reading |
| **Preset 4** | High saturation, yellows and blues darker, good readability |
| **Preset 5** | Medium saturation, CMY colors slightly desaturated, natural look |
| **Grayscale** | Global grayscale mode, all pixels converted to grayscale by luminance, keeps dark tones |

## Configuration

Config file location: `%AppData%\DarkReader\settings.json`

```json
{
  "ActiveMode": 0,
  "ActiveOnStartup": false,
  "SmoothTransitions": true,
  "UseRegion": false,
  "RegionX": 0,
  "RegionY": 0,
  "RegionWidth": 0,
  "RegionHeight": 0,
  "UseWindow": false,
  "TargetWindowTitle": "",
  "PauseWhenNotInForeground": true
}
```

| Field | Description |
|-------|-------------|
| `ActiveMode` | Current mode (0=off, 1=Default, 2-6=Preset 1-5, 7=Grayscale) |
| `ActiveOnStartup` | Whether to auto-enable on launch |
| `SmoothTransitions` | Whether to enable 150ms smooth transition animation |
| `UseRegion` | Whether region restriction is enabled |
| `RegionX` | Region top-left X coordinate |
| `RegionY` | Region top-left Y coordinate |
| `RegionWidth` | Region width |
| `RegionHeight` | Region height |
| `UseWindow` | Whether window target mode is enabled |
| `TargetWindowTitle` | Target window title (for display) |
| `PauseWhenNotInForeground` | Whether to auto-pause when window loses focus |

Settings are auto-saved when switching modes and restored on restart.

## Single Instance

Only one instance is allowed. If DarkReader is already running when you launch it again:
- The new process sends a signal to the existing instance (toggles the switch)
- The new process exits automatically

## System Requirements

- Windows 10/11 64-bit
- No .NET Runtime installation needed (bundled in the exe)
- No administrator privileges required

## Build from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download) or later.

```bash
# Clone the repository
git clone https://github.com/CodingAQ/DarkReader-Desktop.git
cd DarkReader-Desktop

# Restore dependencies and build
dotnet build

# Debug run
dotnet run --project DarkReader

# Publish single-file self-contained version (~68MB)
dotnet publish DarkReader -c Release -r win-x64 --self-contained true -o Release
```

Build output goes to the `Release/` directory. See [CONTRIBUTING.md](./CONTRIBUTING.md) for details.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| No screen change after launch | Ensure Windows DWM/Aero is enabled |
| Hotkeys not responding | Check if another app occupies the same hotkeys |
| Tray icon not showing | Check Windows notification area settings, ensure DarkReader is not hidden |
| Screen still inverted after exit | Restart the app and press `Win + Alt + N` to turn off, or restart Windows |

## Uninstall

1. Ensure DarkReader has exited (right-click tray icon → Exit)
2. Delete the program folder
3. (Optional) Delete config: `%AppData%\DarkReader\`

## Acknowledgments

- [NegativeScreen](https://github.com/mlaily/NegativeScreen) by [mlaily](https://github.com/mlaily) — the upstream foundation of this project
- Tom MacLeod — inspiration for the Smart Inversion algorithm

## License

This project is released under the [GPL-3.0](./LICENSE) license. Use, modification, and distribution must comply with the license terms.

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).
