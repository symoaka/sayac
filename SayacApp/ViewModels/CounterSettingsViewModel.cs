using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SayacApp.Models;
using SayacApp.Services;

namespace SayacApp.ViewModels;

public partial class CounterSettingsViewModel : ObservableObject
{
    public CounterViewModel Counter { get; }
    public string[] PaletteColors => Palette.Colors;
    public LocalizationService Loc => LocalizationService.Instance;

    public CounterSettingsViewModel(CounterViewModel counter) => Counter = counter;

    public DateTimeOffset? TargetDate
    {
        get => Counter.TargetUtc.ToLocalTime();
        set
        {
            if (value is null) return;
            UpdateTarget(value.Value.LocalDateTime.Date, Counter.TargetUtc.ToLocalTime().TimeOfDay);
        }
    }

    public TimeSpan? TargetTime
    {
        get => Counter.TargetUtc.ToLocalTime().TimeOfDay;
        set
        {
            if (value is null) return;
            UpdateTarget(Counter.TargetUtc.ToLocalTime().LocalDateTime.Date, value.Value);
        }
    }

    private void UpdateTarget(DateTime localDate, TimeSpan localTime)
    {
        Counter.TargetUtc = TimeService.LocalToUtc(localDate, localTime);
        OnPropertyChanged(nameof(TargetDate));
        OnPropertyChanged(nameof(TargetTime));
    }

    [RelayCommand]
    private void SetBg(string color)
    {
        Counter.BgColor = color;
        Counter.BgTransparent = false;
    }

    [RelayCommand]
    private void SetText(string color) => Counter.TextColor = color;

    [RelayCommand]
    private void SetTransparent() => Counter.BgTransparent = true;
}
