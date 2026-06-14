using System;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SayacApp.Models;
using SayacApp.Services;

namespace SayacApp.ViewModels;

public partial class AppSettingsViewModel : ObservableObject
{
    private readonly HotkeyService _hotkeys;

    public AppSettings Settings { get; }
    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private string _status = "";

    /// <summary>Raised when a non-observable setting (a hotkey) changed, so the app can save.</summary>
    public event Action? Changed;

    public AppSettingsViewModel(AppSettings settings, HotkeyService hotkeys)
    {
        Settings = settings;
        _hotkeys = hotkeys;
        _status = Loc["HotkeyHint"];
    }

    private static string MetaLabel => OperatingSystem.IsMacOS() ? "Cmd" : "Win";

    public string MiniHotkeyText => Settings.MiniHotkey.Display(MetaLabel);
    public string MainHotkeyText => Settings.MainHotkey.Display(MetaLabel);

    public bool IsEnglish
    {
        get => Settings.Language == "en";
        set
        {
            Settings.Language = value ? "en" : "tr";
            OnPropertyChanged();
            OnPropertyChanged(nameof(LanguageIndex));
        }
    }

    /// <summary>ComboBox index 0 = Türkçe, 1 = English.</summary>
    public int LanguageIndex
    {
        get => IsEnglish ? 1 : 0;
        set => IsEnglish = value == 1;
    }

    /// <summary>ComboBox index 0 = System, 1 = Light, 2 = Dark. Persists and applies live.</summary>
    public int ThemeIndex
    {
        get => Settings.ThemeMode switch { "Light" => 1, "Dark" => 2, _ => 0 };
        set
        {
            Settings.ThemeMode = value switch { 1 => "Light", 2 => "Dark", _ => "System" };
            if (Application.Current is { } app)
                app.RequestedThemeVariant = App.ThemeVariantFor(Settings.ThemeMode);
            OnPropertyChanged();
        }
    }

    /// <summary>ComboBox index 0..3 ↔ 1/3/5/All.</summary>
    public int DisplayIndex
    {
        get => Settings.MiniDisplayCount switch { 1 => 0, 3 => 1, 5 => 2, _ => 3 };
        set
        {
            Settings.MiniDisplayCount = value switch { 0 => 1, 1 => 3, 2 => 5, _ => 0 };
            OnPropertyChanged();
        }
    }

    [RelayCommand] private void ListenMini() => BeginCapture(isMini: true);
    [RelayCommand] private void ListenMain() => BeginCapture(isMini: false);

    private void BeginCapture(bool isMini)
    {
        if (!_hotkeys.Active)
        {
            Status = Loc["HotkeyBindFailed"];
            return;
        }
        Status = Loc["PressCombo"];
        _hotkeys.BeginCapture(def => Dispatcher.UIThread.Post(() => ApplyCapture(isMini, def)));
    }

    private void ApplyCapture(bool isMini, HotkeyDefinition? def)
    {
        if (def is null || !def.HasModifier || !def.HasKey)
        {
            Status = Loc["NeedModifier"];
            return;
        }

        var other = isMini ? Settings.MainHotkey : Settings.MiniHotkey;
        if (def.SameAs(other))
        {
            Status = Loc["HotkeyInUse"];
            return;
        }

        if (isMini) Settings.MiniHotkey = def;
        else Settings.MainHotkey = def;

        Status = isMini ? MiniHotkeyText : MainHotkeyText;
        OnPropertyChanged(nameof(MiniHotkeyText));
        OnPropertyChanged(nameof(MainHotkeyText));
        Changed?.Invoke();
    }
}
