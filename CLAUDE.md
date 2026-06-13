# Sayaç — Project Notes

A single-file **AutoHotkey v2** countdown-timer app for Windows. UI is in Turkish.
Everything lives in `sayac.ahk` (~1250 lines). State persists to `sayac_data.ini`
(created next to the script at runtime; not in the repo).

## What it does

- **Tray app** (`MakeTray`): toggle main panel, toggle mini overlay, toggle mini lock,
  open settings, exit.
- **Main panel** (`MakeMainGui` / `RenderCards`): add counters (name + target date/time),
  see live countdowns as cards, per-counter "Ayar" (settings) and "Sil" (delete) buttons.
- **Mini overlay** (`RebuildMini` / `UpdateMini`): always-on-top frameless window showing
  the top N counters. Draggable when unlocked; click-through (`+E0x20`) when locked.
- **Counter settings** (`OpenCounterSettings`): color palette for bg/text, font size,
  auto vs. manual box size, transparency/alpha, X/Y position, pin-to-mini.
- **App settings** (`OpenSettingsPage`): rebind the two global hotkeys, mini lock toggle,
  mini display count (1 / 3 / 5 / all).

## Key conventions / gotchas

- **Targets are stored as Unix seconds**, but with a **+3h (Istanbul/UTC+3) offset baked in**
  — see `FormatTarget`, `TargetToPickerStamp`, `PickerControlsToTarget` (the `3 * 3600`).
  Countdown math uses `NowUnixUtc()`. Keep this offset consistent if you touch time code.
- **`SaveData` rewrites the whole INI** (deletes then re-writes every counter section).
- Several settings (`bgAlpha`, `posX`, `posY`) are applied to **all counters at once**
  in the slider handlers (`OnAlphaChange`, `OnPosXChange`, `OnPosYChange`) — the mini
  window is shared, so position/alpha are effectively global, while colors/font/size are
  per-counter.
- Mini layout is recomputed every second (`UpdateMini` → `MaybeResizeMiniAuto`); auto-size
  rows estimate width from string length × font size (`CalcMiniLayout`).
- Transparent rows use a color-key trick (`TransparentKeyColor` = `010203` via
  `WinSetTransColor`) and only when the mini is **locked**.
- Hotkeys default to `#SC028` (Win + `'`) for mini and `^!s` (Ctrl+Alt+S) for the main panel.
- Values are clamped/validated via `Clamp` and `CleanColor` on both load and save.

## Running / testing

Windows-only (needs AutoHotkey v2). No automated tests. Verify by running the script and
exercising the GUI manually. Editing happens here on macOS; the script runs on Windows.

## Current work / TODO

**Planned rewrite (approved 2026-06-13, not yet started):** rewrite this app as a
modern **Avalonia UI / .NET 8 / C# MVVM** application (cross-platform Windows + macOS).

Agreed decisions:
- **Scope:** feature parity **+ fix known quirks** (real local-time/DST handling;
  make global-vs-per-counter settings explicit — `posX/posY/bgAlpha` are currently
  fake-per-counter and should become global mini settings).
- **Time:** store UTC, display in system local timezone with DST. Drop the hardcoded
  +3h offset.
- **Localization:** Turkish + English with a runtime language toggle (resource-based).
- **Persistence:** JSON in per-OS app-data dir (debounced save), with an optional
  one-time importer for existing `sayac_data.ini`.
- **Platform-specific pieces only:** global hotkeys (SharpHook) and click-through
  overlay (`WS_EX_TRANSPARENT` / macOS `ignoresMouseEvents`).
- **Kept minimal (declined extras):** no completion notification/sound, no autostart,
  no drag-to-reorder, no custom color picker. At zero → show "Tamamlandı"/"Completed".
- **Modernization defaults:** OS system UI font (not hardcoded Segoe UI); enforce
  single instance; close-to-tray; show a message if a hotkey is already taken; ship
  our own tray icon.

Full analysis + architecture: `~/.claude/plans/you-are-helping-me-happy-hippo.md`.

**Status:** rewrite IMPLEMENTED and building/running on macOS. The new app lives in
`sayac/SayacApp/` (Avalonia 11.2 / .NET 8 / C# MVVM). See `SayacApp/README.md`.
The original `sayac.ahk` is kept untouched as reference.

### Building/running the new app
- .NET 8 SDK is installed at `~/.dotnet` (NOT on global PATH). Prefix commands with
  `export PATH="$HOME/.dotnet:$PATH"; export DOTNET_ROOT="$HOME/.dotnet"`.
- `cd SayacApp && dotnet run`. Headless smoke test: `SAYAC_SELFTEST=1 dotnet bin/Debug/net8.0/Sayac.dll`
  (renders then auto-exits after ~4s).
- Data: `~/Library/Application Support/Sayac/settings.json` (macOS) / `%APPDATA%\Sayac` (Windows).

### Verified so far
JSON load/save round-trip; both windows render (pinned + transparent + manual-size
counters); TR/EN; mini saved position; macOS click-through shim (no crash); legacy
INI import with correct +3h→UTC time reconstruction (this machine is UTC+3).

### Known follow-ups / not yet done
- Real interactive UX pass on the actual screen (drag the mini, rebind a hotkey,
  toggle lock/opacity) — only headless smoke-tested so far.
- macOS global hotkeys need Accessibility permission to function.
- Ship a real app icon (currently reuses the Avalonia template logo).
- Slider double↔int bindings may log benign binding warnings.
