using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SayacApp.Views;

public partial class CounterSettingsWindow : Window
{
    public CounterSettingsWindow()
    {
        InitializeComponent();
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
