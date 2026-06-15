using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using SayacApp.Models;

namespace SayacApp.Services;

/// <summary>
/// Loads/saves the app document as JSON. Saves are debounced (sliders fire rapidly),
/// and flushed synchronously on exit so nothing is lost.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // MiniX/MiniY use NaN as the "not yet placed" sentinel.
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    private readonly DispatcherTimer _debounce;
    private AppData? _pending;

    public SettingsStore()
    {
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            if (_pending is not null) WriteNow(_pending);
        };
    }

    public AppData Load() => LoadData();

    /// <summary>
    /// Read the document without constructing a store (no dispatcher timer). Safe to call
    /// from <c>Program.Main</c> before Avalonia is initialized, to read startup-only settings.
    /// </summary>
    public static AppData LoadData()
    {
        try
        {
            if (File.Exists(AppPaths.SettingsFile))
            {
                var json = File.ReadAllText(AppPaths.SettingsFile);
                var data = JsonSerializer.Deserialize<AppData>(json, JsonOpts);
                if (data is not null)
                {
                    data.Settings ??= new AppSettings();
                    data.Counters ??= new();
                    return data;
                }
            }
            else
            {
                // First run: try to import an old AHK INI if one is sitting around.
                var imported = IniImporter.TryImport(AppPaths.LegacyIniGuess);
                if (imported is not null)
                {
                    WriteNow(imported);
                    return imported;
                }
            }
        }
        catch
        {
            // Corrupt/unreadable settings should never block startup.
        }

        return new AppData();
    }

    /// <summary>Schedule a debounced save.</summary>
    public void RequestSave(AppData data)
    {
        _pending = data;
        _debounce.Stop();
        _debounce.Start();
    }

    /// <summary>Write immediately (e.g. on exit).</summary>
    public void Flush(AppData data)
    {
        _debounce.Stop();
        WriteNow(data);
    }

    private static void WriteNow(AppData data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOpts);
            var tmp = AppPaths.SettingsFile + ".tmp";
            File.WriteAllText(tmp, json);
            File.Copy(tmp, AppPaths.SettingsFile, overwrite: true);
            File.Delete(tmp);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[SettingsStore] save failed: " + ex);
        }
    }
}
