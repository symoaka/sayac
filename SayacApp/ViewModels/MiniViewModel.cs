using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SayacApp.Models;

namespace SayacApp.ViewModels;

/// <summary>
/// Derives the ordered, limited set of counters shown in the mini overlay
/// (pinned first, then insertion order, capped by the display count).
/// </summary>
public partial class MiniViewModel : ObservableObject
{
    private readonly ObservableCollection<CounterViewModel> _all;
    private readonly AppSettings _settings;

    public ObservableCollection<CounterViewModel> Visible { get; } = new();

    public double BackgroundOpacity => _settings.MiniOpacity / 255.0;
    public bool Locked => _settings.MiniLocked;

    public bool HasSavedPosition => !double.IsNaN(_settings.MiniX) && !double.IsNaN(_settings.MiniY);
    public double SavedX => _settings.MiniX;
    public double SavedY => _settings.MiniY;

    public void SavePosition(double x, double y)
    {
        _settings.MiniX = x;
        _settings.MiniY = y;
    }

    public MiniViewModel(ObservableCollection<CounterViewModel> all, AppSettings settings)
    {
        _all = all;
        _settings = settings;

        _all.CollectionChanged += OnCollectionChanged;
        foreach (var c in _all) c.PropertyChanged += OnCounterChanged;
        _settings.PropertyChanged += OnSettingsChanged;

        Recompute();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (CounterViewModel c in e.OldItems) c.PropertyChanged -= OnCounterChanged;
        if (e.NewItems is not null)
            foreach (CounterViewModel c in e.NewItems) c.PropertyChanged += OnCounterChanged;
        Recompute();
    }

    private void OnCounterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CounterViewModel.Pinned) ||
            e.PropertyName == nameof(CounterViewModel.ShowInMini))
            Recompute();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppSettings.MiniDisplayCount):
                Recompute();
                break;
            case nameof(AppSettings.MiniOpacity):
                OnPropertyChanged(nameof(BackgroundOpacity));
                break;
            case nameof(AppSettings.MiniLocked):
                OnPropertyChanged(nameof(Locked));
                break;
        }
    }

    public void Recompute()
    {
        var limit = _settings.MiniDisplayCount <= 0 ? int.MaxValue : _settings.MiniDisplayCount;

        var ordered = new System.Collections.Generic.List<CounterViewModel>();
        foreach (var c in _all) if (c.ShowInMini && c.Pinned) ordered.Add(c);
        foreach (var c in _all) if (c.ShowInMini && !c.Pinned) ordered.Add(c);
        if (ordered.Count > limit) ordered.RemoveRange(limit, ordered.Count - limit);

        Visible.Clear();
        foreach (var c in ordered) Visible.Add(c);
    }
}
