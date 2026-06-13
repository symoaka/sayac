# Sayaç (Avalonia rewrite)

Cross-platform (Windows + macOS) rewrite of the original `sayac.ahk` AutoHotkey app.
A tray-resident countdown app with a main management window and an always-on-top
floating "mini" overlay.

## Stack
- .NET 8 + Avalonia UI 11.2, C#, MVVM (CommunityToolkit.Mvvm)
- SharpHook for global hotkeys
- JSON persistence in the per-OS app-data folder

## Run

The .NET 8 SDK on this machine lives in `~/.dotnet` (not on the global PATH), so:

```bash
export PATH="$HOME/.dotnet:$PATH"
cd SayacApp
dotnet run
```

Build only: `dotnet build`. The app starts with the main window + the mini overlay,
and lives in the system tray / menu bar (close the main window → it hides to tray;
quit from the tray menu).

## Project layout
- `Models/` – `Counter`/`CounterData`, `AppSettings`, `HotkeyDefinition`, `Palette`
- `Services/` – `SettingsStore` (JSON, debounced), `IniImporter` (legacy migration),
  `TimeService` (UTC store + local/DST display), `LocalizationService` (TR/EN),
  `HotkeyService` (SharpHook), `ClickThroughService` (Win32 / macOS shims)
- `ViewModels/` – `MainViewModel`, `CounterViewModel`, `MiniViewModel`,
  `CounterSettingsViewModel`, `AppSettingsViewModel`
- `Views/` – `MainWindow`, `MiniWindow`, `CounterSettingsWindow`, `AppSettingsWindow`

## Data location
- Windows: `%APPDATA%\Sayac\settings.json`
- macOS: `~/Library/Application Support/Sayac/settings.json`

On first run, if an old `sayac_data.ini` sits next to the executable it is imported
once (targets converted from the legacy +3h scheme to true UTC).

## Platform notes
- **Global hotkeys on macOS** need Accessibility permission
  (System Settings → Privacy & Security → Accessibility). Without it the app still
  runs; hotkeys are just inactive.
- **Click-through** (locked mini passes mouse clicks through) is implemented on
  Windows (`WS_EX_TRANSPARENT`) and macOS (`NSWindow.ignoresMouseEvents`).
- Tray icon currently reuses the Avalonia template logo as a placeholder.

## Self-test
`SAYAC_SELFTEST=1 dotnet bin/Debug/net8.0/Sayac.dll` starts the app, renders, and
auto-exits after ~4s (used for headless smoke verification).
