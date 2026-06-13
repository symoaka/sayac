using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SayacApp.Models;

namespace SayacApp.Services;

/// <summary>
/// One-time best-effort import of an old AHK <c>sayac_data.ini</c>. The legacy app
/// stored targets as "local wall-clock numerals as unix seconds, plus a 3h offset".
/// We reconstruct the wall-clock the user actually picked and convert it to true UTC.
/// </summary>
public static class IniImporter
{
    private const long LegacyOffsetSeconds = 3 * 3600;

    public static AppData? TryImport(string iniPath)
    {
        try
        {
            if (!File.Exists(iniPath)) return null;

            var sections = Parse(iniPath);
            var data = new AppData();

            if (sections.TryGetValue("global", out var g))
            {
                data.Settings.MiniLocked = ReadInt(g, "miniLocked", 1) != 0;
                data.Settings.MiniDisplayCount = ParseDisplayMode(ReadStr(g, "miniDisplayMode", "3"));
                if (ParseAhkHotkey(ReadStr(g, "hotkey", ""), out var mini)) data.Settings.MiniHotkey = mini;
                if (ParseAhkHotkey(ReadStr(g, "mainHotkey", ""), out var main)) data.Settings.MainHotkey = main;
            }

            var first = true;
            for (var i = 1; ; i++)
            {
                if (!sections.TryGetValue("counter" + i, out var c)) break;
                var name = ReadStr(c, "name", "");
                var targetRaw = ReadStr(c, "target", "");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(targetRaw)) break;
                if (!long.TryParse(targetRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var stored)) break;

                data.Counters.Add(new CounterData
                {
                    Name = name,
                    TargetUtc = LegacyTargetToUtc(stored),
                    BgColor = CleanColor(ReadStr(c, "bgColor", "0f0f0f"), "0f0f0f"),
                    TextColor = CleanColor(ReadStr(c, "textColor", "FFFFFF"), "FFFFFF"),
                    BgTransparent = ReadInt(c, "bgTransparent", 0) != 0,
                    AutoBoxSize = ReadInt(c, "autoBoxSize", 1) != 0,
                    Pinned = ReadInt(c, "pinned", 0) != 0,
                    FontSize = Clamp(ReadInt(c, "fontSize", 15), 8, 60),
                    BoxWidth = Math.Max(180, ReadInt(c, "boxWidth", 360)),
                    RowHeight = Clamp(ReadInt(c, "rowHeight", 26), 18, 120),
                });

                if (first)
                {
                    // Old per-counter fields that were really global.
                    data.Settings.MiniOpacity = Clamp(ReadInt(c, "bgAlpha", 220), 0, 255);
                    data.Settings.MiniX = ReadInt(c, "posX", int.MinValue) is var px && px != int.MinValue ? px : double.NaN;
                    data.Settings.MiniY = ReadInt(c, "posY", int.MinValue) is var py && py != int.MinValue ? py : double.NaN;
                    first = false;
                }
            }

            return data.Counters.Count > 0 ? data : null;
        }
        catch
        {
            return null;
        }
    }

    private static DateTimeOffset LegacyTargetToUtc(long stored)
    {
        // stored = wallNumeralsUnix + 3h. Recover wall-clock numerals, treat as local, → UTC.
        var wallUnix = stored - LegacyOffsetSeconds;
        var wallNumerals = DateTimeOffset.FromUnixTimeSeconds(wallUnix).UtcDateTime;
        var asLocal = DateTime.SpecifyKind(wallNumerals, DateTimeKind.Local);
        return new DateTimeOffset(asLocal).ToUniversalTime();
    }

    private static int ParseDisplayMode(string v) => v.Trim().ToLowerInvariant() switch
    {
        "1" => 1, "5" => 5, "all" => 0, _ => 3
    };

    private static bool ParseAhkHotkey(string ahk, out HotkeyDefinition def)
    {
        def = new HotkeyDefinition();
        if (string.IsNullOrWhiteSpace(ahk)) return false;

        var i = 0;
        for (; i < ahk.Length; i++)
        {
            switch (ahk[i])
            {
                case '#': def.Meta = true; continue;
                case '^': def.Ctrl = true; continue;
                case '!': def.Alt = true; continue;
                case '+': def.Shift = true; continue;
            }
            break;
        }

        var key = ahk[i..].Trim();
        if (key.Length == 0) return false;

        if (key.Equals("SC028", StringComparison.OrdinalIgnoreCase)) def.Key = "VcQuote";
        else if (key.Length == 1 && char.IsLetter(key[0])) def.Key = "Vc" + char.ToUpperInvariant(key[0]);
        else if (key.Length == 1 && char.IsDigit(key[0])) def.Key = "Vc" + key;
        else if (key.Length is 2 or 3 && (key[0] == 'F' || key[0] == 'f') && int.TryParse(key[1..], out _)) def.Key = "VcF" + key[1..];
        else return false;

        return def.HasModifier;
    }

    private static string CleanColor(string value, string fallback)
    {
        value = value.Replace("#", "").Trim();
        return value.Length == 6 && IsHex(value) ? value : fallback;
    }

    private static bool IsHex(string s)
    {
        foreach (var ch in s)
            if (!Uri.IsHexDigit(ch)) return false;
        return true;
    }

    private static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;

    private static string ReadStr(Dictionary<string, string> sec, string key, string fallback) =>
        sec.TryGetValue(key.ToLowerInvariant(), out var v) ? v : fallback;

    private static int ReadInt(Dictionary<string, string> sec, string key, int fallback) =>
        sec.TryGetValue(key.ToLowerInvariant(), out var v) &&
        int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : fallback;

    private static Dictionary<string, Dictionary<string, string>> Parse(string path)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var current = "";
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';')) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = line[1..^1].Trim();
                if (!result.ContainsKey(current)) result[current] = new(StringComparer.OrdinalIgnoreCase);
                continue;
            }
            var eq = line.IndexOf('=');
            if (eq <= 0 || current.Length == 0) continue;
            var k = line[..eq].Trim().ToLowerInvariant();
            var val = line[(eq + 1)..].Trim();
            result[current][k] = val;
        }
        return result;
    }
}
