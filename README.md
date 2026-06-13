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

## Getting started

There are no prebuilt downloads yet, so you build it from source. It's three steps
and takes a couple of minutes. No prior .NET experience needed.

### 1. Install the .NET 8 SDK

The SDK is the free toolkit that builds and runs the app.

- Download it from **https://dotnet.microsoft.com/download/dotnet/8.0** — pick the
  **SDK** (not just the "Runtime") for your operating system and run the installer.
- On macOS you can instead use Homebrew: `brew install dotnet-sdk`
- On Windows you can instead use winget: `winget install Microsoft.DotNet.SDK.8`

Then open a **new** terminal and confirm it's installed:

```bash
dotnet --version
```

You should see a version starting with `8.` (e.g. `8.0.422`). If you get
"command not found", close and reopen your terminal, or restart your computer so the
installer's PATH changes take effect.

### 2. Get the code

If you have **git**:

```bash
git clone https://github.com/symoaka/sayac.git
cd sayac
```

No git? On the GitHub page click the green **Code** button → **Download ZIP**, then
unzip it and open a terminal in the unzipped folder.

### 3. Run it

```bash
cd SayacApp
dotnet run
```

The first run downloads dependencies and compiles, so it takes a little longer — that's
normal. After that the main window and the mini overlay open, and the app lives in the
system tray (Windows) / menu bar (macOS). **Closing the main window hides it to the
tray** rather than quitting; use the tray/menu-bar icon to quit.

To stop the app from the terminal, press `Ctrl + C`.

### Build without running

To produce a binary instead of launching:

```bash
cd SayacApp
dotnet build
```

The result lands in `SayacApp/bin/Debug/net8.0/`.

### Troubleshooting

- **`dotnet: command not found`** — the SDK isn't installed or your terminal hasn't
  picked it up yet. Reopen the terminal (or reboot) after installing.
- **A different .NET version is installed (e.g. 9.x)** — that's fine; the .NET 8 SDK
  can sit alongside others. Just make sure the .NET **8** SDK is present
  (`dotnet --list-sdks` should list an `8.x` entry).
- **Hotkeys don't work on macOS** — grant Accessibility permission (see
  [Platform notes](#platform-notes)).
- **macOS "app is from an unidentified developer"** — this only applies to packaged
  apps. Running from source with `dotnet run` does not trigger it.

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
