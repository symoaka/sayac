# Sayaç

A lightweight **countdown-timer** app with an always-on-top floating overlay.
Define named counters with a target date/time and watch the time remaining tick
down — both in a management window and in a small, draggable mini overlay that
stays on top of everything.

Cross-platform (**Windows + macOS**), built with **Avalonia UI / .NET 8 / C#**
using the MVVM pattern.

> This is a modern rewrite of an original single-file AutoHotkey v2 app
> (Windows-only).

## Features

- **Multiple counters** — name + target date/time, live "time remaining" updated
  every second. At zero it shows **Completed / Tamamlandı**.
- **Mini overlay** — frameless, always-on-top window showing your top counters.
  Drag it anywhere when unlocked; **lock it** to make it click-through (mouse
  passes straight to whatever is behind it).
- **Per-counter styling** — background and text color from a 24-swatch palette,
  font size, transparent background, and auto or manual box sizing.
- **Pinning** — pinned counters always appear first in the mini overlay.
- **Global hotkeys** — toggle the mini overlay and the main window from anywhere
  (rebindable in settings).
- **Bilingual UI** — Turkish and English with a live language toggle.
- **System tray / menu-bar resident** — close the main window and it hides to the
  tray; quit from the tray menu.

## Run

Requires the **.NET 8 SDK**.

```bash
cd SayacApp
dotnet run
```

The app opens the main window plus the mini overlay and lives in the system tray
(Windows) / menu bar (macOS).

To build a binary instead of running: `dotnet build` (output under
`SayacApp/bin/`).

## Data & persistence

Settings and counters are saved as JSON in the per-OS app-data folder (saves are
debounced, not written on every keystroke):

- **Windows:** `%APPDATA%\Sayac\settings.json`
- **macOS:** `~/Library/Application Support/Sayac/settings.json`

On first run, if a legacy `sayac_data.ini` from the old AutoHotkey app sits next
to the executable, it is imported once (old targets are converted from the
legacy +3h scheme to true UTC).

Time is stored in **UTC** and displayed in your **system local timezone with DST**
handling.

## Platform notes

- **Global hotkeys on macOS** require Accessibility permission
  (System Settings → Privacy & Security → Accessibility). Without it the app still
  runs — the hotkeys are just inactive.
- **Click-through** for the locked mini overlay uses `WS_EX_TRANSPARENT` on
  Windows and `NSWindow.ignoresMouseEvents` on macOS.

## Project layout

```
SayacApp/
  Models/        Counter/CounterData, AppSettings, HotkeyDefinition, Palette
  Services/      SettingsStore (JSON, debounced), IniImporter (legacy migration),
                 TimeService (UTC store + local/DST display), LocalizationService
                 (TR/EN), HotkeyService (SharpHook), ClickThroughService (Win/macOS)
  ViewModels/    Main, Counter, Mini, CounterSettings, AppSettings
  Views/         MainWindow, MiniWindow, CounterSettingsWindow, AppSettingsWindow
```

## Stack

.NET 8 · Avalonia UI 11.2 · CommunityToolkit.Mvvm · SharpHook (global hotkeys) ·
System.Text.Json
