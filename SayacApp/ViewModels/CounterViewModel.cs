using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Transformation;
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
    private DateTimeOffset _createdUtc;

    // --- runtime-only ---
    [ObservableProperty][NotifyPropertyChangedFor(nameof(RowText))] private string _remainingText = "";
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private string _targetText = "";
    /// <summary>Days portion of the countdown ("364 gün"), empty under a day.</summary>
    [ObservableProperty] private string _remainingBig = "";
    /// <summary>"HH:mm:ss" portion (empty when completed).</summary>
    [ObservableProperty] private string _remainingClock = "";

    // --- countdown progress glow ---
    // Color-dependent brushes: rebuilt only when BgColor / BgTransparent change (not per tick).
    [ObservableProperty] private IBrush _glowBrush = Brushes.Transparent;
    [ObservableProperty] private IBrush _glowBorderBrush = Brushes.Transparent;
    [ObservableProperty] private BoxShadows _glowShadow;
    // Per-tick: a cheap GPU scaleX the fill border eases toward (compositor transition).
    [ObservableProperty] private ITransform _glowTransform = TransformOperations.Parse("scaleX(0)");
    private double _lastScale = -1;

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
        _createdUtc = d.CreatedUtc;
        RebuildGlowColors();
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
        CreatedUtc = _createdUtc,
    };

    public void Refresh(DateTimeOffset nowUtc)
    {
        RemainingText = TimeService.FormatRemaining(TargetUtc, nowUtc);
        (RemainingBig, RemainingClock) = TimeService.FormatRemainingParts(TargetUtc, nowUtc);
        IsCompleted = TimeService.IsCompleted(TargetUtc, nowUtc);
        TargetText = TimeService.FormatTarget(TargetUtc);

        // Cheap per-tick work: only nudge the scaleX target, and only when it moved enough
        // to matter (a multi-day countdown barely changes per second → skipped entirely).
        var p = Progress(nowUtc);
        if (Math.Abs(p - _lastScale) >= 0.001 || (p >= 1 && _lastScale < 1))
        {
            _lastScale = p;
            GlowTransform = TransformOperations.Parse(
                "scaleX(" + p.ToString("0.####", CultureInfo.InvariantCulture) + ")");
        }
    }

    partial void OnBgColorChanged(string value) => RebuildGlowColors();
    partial void OnBgTransparentChanged(bool value) => RebuildGlowColors();

    /// <summary>Rebuilds the (static) color-dependent glow brushes from the counter's background color.</summary>
    private void RebuildGlowColors()
    {
        if (BgTransparent)
        {
            GlowBrush = GlowBorderBrush = Brushes.Transparent;
            GlowShadow = default;
            return;
        }

        var c = ParseColor(BgColor);
        var trans = Color.FromArgb(0x00, c.R, c.G, c.B);

        // Fade from transparent (left) to the color (right); the fill border is scaled
        // per tick so the colored region grows from the right edge as time elapses.
        var b = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        };
        b.GradientStops.Add(new GradientStop(trans, 0));
        b.GradientStops.Add(new GradientStop(Color.FromArgb(0xC0, c.R, c.G, c.B), 0.55));
        b.GradientStops.Add(new GradientStop(Color.FromArgb(0xE6, c.R, c.G, c.B), 1));
        GlowBrush = b;

        GlowBorderBrush = new SolidColorBrush(Color.FromArgb(0x66, c.R, c.G, c.B));
        GlowShadow = new BoxShadows(new BoxShadow
        {
            Blur = 16, Spread = 0, OffsetX = 0, OffsetY = 0,
            Color = Color.FromArgb(0x4D, c.R, c.G, c.B),
        });
    }

    /// <summary>0 when just created, 1 at (or past) the target — time elapsed over total duration.</summary>
    private double Progress(DateTimeOffset nowUtc)
    {
        var total = (TargetUtc - _createdUtc).TotalSeconds;
        if (total <= 0) return nowUtc >= TargetUtc ? 1.0 : 0.0;
        return Math.Clamp((nowUtc - _createdUtc).TotalSeconds / total, 0.0, 1.0);
    }

    private static Color ParseColor(string hex)
    {
        try { return Color.Parse("#" + hex.Replace("#", "")); }
        catch { return Color.Parse("#E2453C"); }
    }

    private static IBrush ToBrush(string hex, string fallback)
    {
        try { return new SolidColorBrush(Color.Parse("#" + hex.Replace("#", ""))); }
        catch { return new SolidColorBrush(Color.Parse("#" + fallback)); }
    }
}
