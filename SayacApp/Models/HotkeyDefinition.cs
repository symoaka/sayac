using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SayacApp.Models;

/// <summary>
/// A global hotkey expressed in a platform-neutral way: a set of modifiers plus a
/// single main key. The key name is a <c>SharpHook.Native.KeyCode</c> enum name
/// (e.g. "VcS", "VcQuote") so it round-trips cleanly to JSON and to the global hook.
/// </summary>
public sealed class HotkeyDefinition
{
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public bool Meta { get; set; }

    /// <summary>SharpHook KeyCode enum name, e.g. "VcS".</summary>
    public string Key { get; set; } = "";

    [JsonIgnore] public bool HasKey => !string.IsNullOrEmpty(Key);
    [JsonIgnore] public bool HasModifier => Ctrl || Alt || Shift || Meta;

    public HotkeyDefinition Clone() => new()
    {
        Ctrl = Ctrl, Alt = Alt, Shift = Shift, Meta = Meta, Key = Key
    };

    public bool SameAs(HotkeyDefinition? other) =>
        other is not null && Ctrl == other.Ctrl && Alt == other.Alt &&
        Shift == other.Shift && Meta == other.Meta &&
        string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);

    /// <summary>Human readable label, e.g. "Ctrl+Alt+S". <paramref name="metaLabel"/>
    /// is "Win" on Windows / "Cmd" on macOS.</summary>
    public string Display(string metaLabel = "Win")
    {
        var parts = new List<string>();
        if (Meta) parts.Add(metaLabel);
        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        parts.Add(KeyLabels.TryGetValue(Key, out var lbl) ? lbl : StripVc(Key));
        return string.Join("+", parts);
    }

    private static string StripVc(string key) =>
        key.StartsWith("Vc", StringComparison.Ordinal) ? key[2..] : key;

    /// <summary>Friendly labels for the handful of keys that don't read well raw.</summary>
    public static readonly Dictionary<string, string> KeyLabels = new()
    {
        ["VcQuote"] = "'",
        ["VcSpace"] = "Space",
        ["VcEnter"] = "Enter",
        ["VcTab"] = "Tab",
        ["VcEscape"] = "Esc",
        ["VcBackspace"] = "Backspace",
        ["VcDelete"] = "Delete",
        ["VcInsert"] = "Insert",
        ["VcHome"] = "Home",
        ["VcEnd"] = "End",
        ["VcPageUp"] = "PageUp",
        ["VcPageDown"] = "PageDown",
        ["VcUp"] = "Up",
        ["VcDown"] = "Down",
        ["VcLeft"] = "Left",
        ["VcRight"] = "Right",
    };
}
