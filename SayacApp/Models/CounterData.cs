using System;

namespace SayacApp.Models;

/// <summary>
/// Plain serializable record of a counter (what gets written to settings.json).
/// The live, bindable version is <c>ViewModels.CounterViewModel</c>.
/// </summary>
public sealed class CounterData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";

    /// <summary>Target instant stored in UTC. Display converts to local time + DST.</summary>
    public DateTimeOffset TargetUtc { get; set; } = DateTimeOffset.UtcNow;

    public string BgColor { get; set; } = "0f0f0f";
    public string TextColor { get; set; } = "FFFFFF";
    public int FontSize { get; set; } = 15;
    public bool AutoBoxSize { get; set; } = true;
    public int BoxWidth { get; set; } = 360;
    public int RowHeight { get; set; } = 26;
    public bool BgTransparent { get; set; }
    public bool Pinned { get; set; }
}

/// <summary>Root persisted document.</summary>
public sealed class AppData
{
    public AppSettings Settings { get; set; } = new();
    public System.Collections.Generic.List<CounterData> Counters { get; set; } = new();
}
