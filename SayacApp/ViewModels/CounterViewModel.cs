using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SayacApp.Models;
using SayacApp.Services;

namespace SayacApp.ViewModels;

/// <summary>
/// Live, bindable counter used by both the main cards and the mini overlay (same
/// instance in both, so one timer tick updates everywhere).
/// </summary>
public partial class CounterViewModel : ObservableObject
{
    public Guid Id { get; }

    // --- persisted fields ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowText))]
    [NotifyPropertyChangedFor(nameof(InitialChar))]
    private string _name = "";
    [ObservableProperty] private DateTimeOffset _targetUtc;
    [ObservableProperty] private string _icon = "";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(BackgroundBrush))] private string _bgColor = "0f0f0f";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(TextBrush))] private string _textColor = "FFFFFF";
    [ObservableProperty] private int _fontSize = 15;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManualWidth))]
    [NotifyPropertyChangedFor(nameof(ManualHeight))]
    private bool _autoBoxSize = true;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ManualWidth))] private int _boxWidth = 360;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ManualHeight))] private int _rowHeight = 26;
    [ObservableProperty] private bool _bgTransparent;
    [ObservableProperty] private bool _pinned;
    [ObservableProperty] private bool _showInMini = true;

    // --- runtime-only ---
    [ObservableProperty][NotifyPropertyChangedFor(nameof(RowText))] private string _remainingText = "";
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private string _targetText = "";
    /// <summary>Days portion of the countdown ("364 gün"), empty under a day.</summary>
    [ObservableProperty] private string _remainingBig = "";
    /// <summary>"HH:mm:ss" portion (empty when completed).</summary>
    [ObservableProperty] private string _remainingClock = "";

    public string RowText => $"{Name}: {RemainingText}";
    public IBrush BackgroundBrush => ToBrush(BgColor, "0f0f0f");
    public IBrush TextBrush => ToBrush(TextColor, "FFFFFF");

    /// <summary>Tile chip fallback when no emoji is set: first non-space character of the name.</summary>
    public string InitialChar
    {
        get
        {
            var n = (Name ?? "").Trim();
            return n.Length > 0 ? n.Substring(0, 1).ToUpperInvariant() : "•";
        }
    }

    // NaN = "auto" in Avalonia layout; manual sizing only when AutoBoxSize is off.
    public double ManualWidth => AutoBoxSize ? double.NaN : BoxWidth;
    public double ManualHeight => AutoBoxSize ? double.NaN : RowHeight;

    /// <summary>Property names that should trigger a save when they change.</summary>
    public static readonly System.Collections.Generic.HashSet<string> PersistedProps = new()
    {
        nameof(Name), nameof(Icon), nameof(TargetUtc), nameof(BgColor), nameof(TextColor),
        nameof(FontSize), nameof(AutoBoxSize), nameof(BoxWidth), nameof(RowHeight),
        nameof(BgTransparent), nameof(Pinned), nameof(ShowInMini)
    };

    public CounterViewModel(CounterData d)
    {
        Id = d.Id;
        _name = d.Name;
        _icon = d.Icon;
        _targetUtc = d.TargetUtc;
        _bgColor = d.BgColor;
        _textColor = d.TextColor;
        _fontSize = d.FontSize;
        _autoBoxSize = d.AutoBoxSize;
        _boxWidth = d.BoxWidth;
        _rowHeight = d.RowHeight;
        _bgTransparent = d.BgTransparent;
        _pinned = d.Pinned;
        _showInMini = d.ShowInMini;
        Refresh(DateTimeOffset.UtcNow);
    }

    public CounterData ToData() => new()
    {
        Id = Id,
        Name = Name,
        Icon = Icon,
        TargetUtc = TargetUtc,
        BgColor = BgColor,
        TextColor = TextColor,
        FontSize = FontSize,
        AutoBoxSize = AutoBoxSize,
        BoxWidth = BoxWidth,
        RowHeight = RowHeight,
        BgTransparent = BgTransparent,
        Pinned = Pinned,
        ShowInMini = ShowInMini,
    };

    public void Refresh(DateTimeOffset nowUtc)
    {
        RemainingText = TimeService.FormatRemaining(TargetUtc, nowUtc);
        (RemainingBig, RemainingClock) = TimeService.FormatRemainingParts(TargetUtc, nowUtc);
        IsCompleted = TimeService.IsCompleted(TargetUtc, nowUtc);
        TargetText = TimeService.FormatTarget(TargetUtc);
    }

    private static IBrush ToBrush(string hex, string fallback)
    {
        try { return new SolidColorBrush(Color.Parse("#" + hex.Replace("#", ""))); }
        catch { return new SolidColorBrush(Color.Parse("#" + fallback)); }
    }
}
