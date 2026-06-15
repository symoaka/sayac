using CommunityToolkit.Mvvm.ComponentModel;

namespace SayacApp.Models;

/// <summary>
/// Global application settings. Note: mini overlay position and opacity live here
/// (they are genuinely global), fixing the old app's "fake per-counter" quirk where
/// posX/posY/bgAlpha were copied onto every counter.
/// </summary>
public partial class AppSettings : ObservableObject
{
    [ObservableProperty] private string _language = "tr";          // "tr" | "en"

    /// <summary>UI theme: "System" follows the OS, otherwise "Light" | "Dark".</summary>
    [ObservableProperty] private string _themeMode = "Dark";

    /// <summary>When true, counter tiles drop the animated progress glow for a clean, flat look.</summary>
    [ObservableProperty] private bool _minimalTiles;

    /// <summary>
    /// When true, the app uses CPU/software rendering instead of the GPU compositor,
    /// cutting ~90 MB of graphics memory. Read once at startup (see Program.cs); changing
    /// it only takes effect on the next launch.
    /// </summary>
    [ObservableProperty] private bool _performanceMode;

    [ObservableProperty] private bool _miniVisible = true;
    [ObservableProperty] private bool _miniLocked = true;

    /// <summary>0 = all, otherwise the max number of rows shown in the mini (1/3/5).</summary>
    [ObservableProperty] private int _miniDisplayCount = 3;

    /// <summary>Global mini background opacity, 0-255 (matches the old bgAlpha range).</summary>
    [ObservableProperty] private int _miniOpacity = 220;

    /// <summary>Saved mini position. NaN means "not yet placed" (use default corner).</summary>
    [ObservableProperty] private double _miniX = double.NaN;
    [ObservableProperty] private double _miniY = double.NaN;

    public HotkeyDefinition MiniHotkey { get; set; } = new() { Meta = true, Key = "VcQuote" };
    public HotkeyDefinition MainHotkey { get; set; } = new() { Ctrl = true, Alt = true, Key = "VcS" };
}
