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

    /// <summary>Preset emojis offered as quick-pick chips in the settings dialog.</summary>
    public string[] IconChoices { get; } =
        { "🎯", "📚", "✈️", "🎂", "💼", "📝", "🏁", "🎉", "⏰", "🚀", "🎓", "❤️" };

    public CounterSettingsViewModel(CounterViewModel counter) => Counter = counter;

    /// <summary>Summary shown on the "When" popover button, e.g. "14 Jun 2026 · 15:12".</summary>
    public string WhenSummary => Counter.TargetUtc.ToLocalTime().ToString("dd MMM yyyy · HH:mm");

    public DateTimeOffset? TargetDate
    {
        get => Counter.TargetUtc.ToLocalTime();
        set
        {
            if (value is null) return;
            UpdateTarget(value.Value.LocalDateTime.Date, Counter.TargetUtc.ToLocalTime().TimeOfDay);
        }
    }

    /// <summary>DateTime?-typed mirror of TargetDate for CalendarDatePicker.SelectedDate.</summary>
    public DateTime? CalendarDate
    {
        get => Counter.TargetUtc.ToLocalTime().Date;
        set
        {
            if (value is null) return;
            UpdateTarget(value.Value.Date, Counter.TargetUtc.ToLocalTime().TimeOfDay);
        }
    }

    public decimal TargetHour
    {
        get => Counter.TargetUtc.ToLocalTime().Hour;
        set
        {
            var local = Counter.TargetUtc.ToLocalTime();
            var h = Math.Clamp((int)value, 0, 23);
            UpdateTarget(local.Date, new TimeSpan(h, local.Minute, 0));
        }
    }

    public decimal TargetMinute
    {
        get => Counter.TargetUtc.ToLocalTime().Minute;
        set
        {
            var local = Counter.TargetUtc.ToLocalTime();
            var m = Math.Clamp((int)value, 0, 59);
            UpdateTarget(local.Date, new TimeSpan(local.Hour, m, 0));
        }
    }

    private void UpdateTarget(DateTime localDate, TimeSpan localTime)
    {
        Counter.TargetUtc = TimeService.LocalToUtc(localDate, localTime);
        RaiseTargetChanged();
    }

    private void RaiseTargetChanged()
    {
        OnPropertyChanged(nameof(TargetDate));
        OnPropertyChanged(nameof(CalendarDate));
        OnPropertyChanged(nameof(TargetHour));
        OnPropertyChanged(nameof(TargetMinute));
        OnPropertyChanged(nameof(WhenSummary));
    }

    // Quick on-the-fly nudges of the target time (no need to retype a date).
    [RelayCommand] private void AddOneMinute() => AdjustTarget(1);
    [RelayCommand] private void AddFiveMinutes() => AdjustTarget(5);
    [RelayCommand] private void AddOneHour() => AdjustTarget(60);
    [RelayCommand] private void SubtractOneMinute() => AdjustTarget(-1);
    [RelayCommand] private void SubtractFiveMinutes() => AdjustTarget(-5);
    [RelayCommand] private void SubtractOneHour() => AdjustTarget(-60);

    private void AdjustTarget(int minutes)
    {
        Counter.TargetUtc = Counter.TargetUtc.AddMinutes(minutes);
        RaiseTargetChanged();
    }

    [RelayCommand]
    private void SetIcon(string icon) => Counter.Icon = icon;

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
