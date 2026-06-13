using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SayacApp.Services;
using SayacApp.ViewModels;

namespace SayacApp.Views;

public partial class MiniWindow : Window
{
    private MiniViewModel? Vm => DataContext as MiniViewModel;

    public MiniWindow()
    {
        InitializeComponent();
        PositionChanged += OnPositionMoved;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (Vm is not null)
            Vm.PropertyChanged += (_, ev) =>
            {
                if (ev.PropertyName == nameof(MiniViewModel.Locked)) ApplyLock();
            };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        RestorePosition();
        ApplyLock();
    }

    private void RestorePosition()
    {
        if (Vm?.HasSavedPosition == true)
        {
            Position = new PixelPoint((int)Vm.SavedX, (int)Vm.SavedY);
            return;
        }

        var wa = Screens.Primary?.WorkingArea;
        if (wa is { } area)
        {
            var w = Bounds.Width > 0 ? (int)Bounds.Width : 360;
            var h = Bounds.Height > 0 ? (int)Bounds.Height : 60;
            Position = new PixelPoint(
                area.X + area.Width - w - 20,
                area.Y + area.Height - h - 60);
        }
    }

    private void ApplyLock() => ClickThroughService.Apply(this, Vm?.Locked ?? true);

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm?.Locked == false && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnPositionMoved(object? sender, PixelPointEventArgs e)
    {
        if (Vm?.Locked == false)
            Vm.SavePosition(e.Point.X, e.Point.Y);
    }
}
