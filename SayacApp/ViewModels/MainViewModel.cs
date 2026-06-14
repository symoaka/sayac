using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SayacApp.Models;
using SayacApp.Services;

namespace SayacApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private readonly DispatcherTimer _tick;

    public AppSettings Settings { get; }
    public ObservableCollection<CounterViewModel> Counters { get; } = new();
    public MiniViewModel Mini { get; }
    public LocalizationService Loc => LocalizationService.Instance;

    // New-counter input
    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private DateTimeOffset? _newDate = DateTimeOffset.Now;
    [ObservableProperty] private TimeSpan? _newTime = DateTime.Now.TimeOfDay;

    [ObservableProperty] private bool _hasCounters;

    /// <summary>Summary shown on the "When" popover button, e.g. "14 Jun 2026 · 15:12".</summary>
    public string NewWhenSummary
    {
        get
        {
            if (NewDate is null || NewTime is null) return "—";
            var dt = NewDate.Value.Date + NewTime.Value;
            return dt.ToString("dd MMM yyyy · HH:mm");
        }
    }

    partial void OnNewDateChanged(DateTimeOffset? value) => OnPropertyChanged(nameof(NewWhenSummary));
    partial void OnNewTimeChanged(TimeSpan? value) => OnPropertyChanged(nameof(NewWhenSummary));

    public event Action<CounterViewModel>? EditCounterRequested;
    public event Action? OpenSettingsRequested;
    public event Action<string, string>? MessageRequested;

    public MainViewModel(AppData data, SettingsStore store)
    {
        _store = store;
        Settings = data.Settings;
        LocalizationService.Instance.Language = Settings.Language;

        foreach (var cd in data.Counters)
            AttachCounter(new CounterViewModel(cd));

        Mini = new MiniViewModel(Counters, Settings);

        Settings.PropertyChanged += OnSettingsChanged;

        _tick = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tick.Tick += (_, _) => RefreshAll();
        _tick.Start();

        RefreshAll();
        UpdateHasCounters();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.Language))
            LocalizationService.Instance.Language = Settings.Language;
        RequestSave();
    }

    [RelayCommand]
    private void Add()
    {
        var name = (NewName ?? "").Trim();
        if (name.Length == 0)
        {
            MessageRequested?.Invoke(Loc["Warning"], Loc["NameEmpty"]);
            return;
        }
        if (NewDate is null || NewTime is null)
        {
            MessageRequested?.Invoke(Loc["Warning"], Loc["NameEmpty"]);
            return;
        }

        var targetUtc = TimeService.LocalToUtc(NewDate.Value.LocalDateTime.Date, NewTime.Value);
        // Give each new counter a vibrant identity color (cycles through the colored
        // part of the palette, indices 8..23) so the tiles are colorful out of the box.
        var colored = Palette.Colors;
        var color = colored[8 + (Counters.Count % (colored.Length - 8))];
        AttachCounter(new CounterViewModel(new CounterData
        {
            Name = name,
            TargetUtc = targetUtc,
            BgColor = color,
        }));

        NewName = "";
        UpdateHasCounters();
        RequestSave();
    }

    [RelayCommand]
    private void DeleteCounter(CounterViewModel? c)
    {
        if (c is null) return;
        c.PropertyChanged -= OnCounterPropChanged;
        Counters.Remove(c);
        UpdateHasCounters();
        RequestSave();
    }

    [RelayCommand]
    private void EditCounter(CounterViewModel? c)
    {
        if (c is not null) EditCounterRequested?.Invoke(c);
    }

    [RelayCommand]
    private void OpenSettings() => OpenSettingsRequested?.Invoke();

    private void AttachCounter(CounterViewModel c)
    {
        c.PropertyChanged += OnCounterPropChanged;
        Counters.Add(c);
    }

    private void OnCounterPropChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not null && CounterViewModel.PersistedProps.Contains(e.PropertyName))
            RequestSave();
    }

    private void RefreshAll()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var c in Counters) c.Refresh(now);
    }

    private void UpdateHasCounters() => HasCounters = Counters.Count > 0;

    public AppData BuildAppData() => new()
    {
        Settings = Settings,
        Counters = Counters.Select(c => c.ToData()).ToList()
    };

    public void RequestSave() => _store.RequestSave(BuildAppData());
    public void Flush() => _store.Flush(BuildAppData());
}
