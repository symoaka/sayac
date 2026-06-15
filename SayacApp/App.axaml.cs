using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using SayacApp.Services;
using SayacApp.ViewModels;
using SayacApp.Views;

namespace SayacApp;

public partial class App : Application
{
    private SettingsStore _store = null!;
    private MainViewModel _main = null!;
    private HotkeyService _hotkeys = null!;

    private MainWindow? _mainWindow;
    private MiniWindow? _miniWindow;
    private AppSettingsWindow? _settingsWindow;
    private CounterSettingsWindow? _counterWindow;

    private TrayIcon? _tray;
    private NativeMenuItem? _miMain, _miMini, _miLock, _miRecover, _miSettings, _miExit;
    private bool _exiting;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>Maps the persisted ThemeMode string to an Avalonia variant ("System" → Default → follows OS).</summary>
    public static ThemeVariant ThemeVariantFor(string mode) => mode switch
    {
        "Light" => ThemeVariant.Light,
        "Dark" => ThemeVariant.Dark,
        _ => ThemeVariant.Default,
    };

    public override void OnFrameworkInitializationCompleted()
    {
        DampenPickerWheel();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _store = new SettingsStore();
            var data = _store.Load();
            _main = new MainViewModel(data, _store);
            RequestedThemeVariant = ThemeVariantFor(data.Settings.ThemeMode);
            _hotkeys = new HotkeyService(_main.Settings);

            _main.EditCounterRequested += OpenCounterSettings;
            _main.OpenSettingsRequested += OpenAppSettings;
            _main.MessageRequested += ShowMessage;

            _mainWindow = new MainWindow { DataContext = _main };
            _mainWindow.Closing += OnMainClosing;
            desktop.MainWindow = _mainWindow;

            _miniWindow = new MiniWindow { DataContext = _main.Mini };

            _hotkeys.MiniTriggered += () => Dispatcher.UIThread.Post(ToggleMini);
            _hotkeys.MainTriggered += () => Dispatcher.UIThread.Post(ToggleMain);
            _hotkeys.Start();

            BuildTray();

            _mainWindow.Show();
            if (_main.Settings.MiniVisible) _miniWindow.Show();

            // Headless smoke test: start, render, then auto-exit so CI/verification
            // doesn't hang on the explicit-shutdown lifetime.
            if (Environment.GetEnvironmentVariable("SAYAC_SELFTEST") == "1")
            {
                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                t.Tick += (_, _) => { t.Stop(); ExitApp(); };
                t.Start();
            }

            desktop.Exit += (_, _) =>
            {
                _hotkeys.Dispose();
                _main.Flush();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// The calendar grid and numeric time steppers navigate/spin on every mouse-wheel
    /// event; on a trackpad a single gesture fires many events and flies through
    /// months/values. Swallow wheel on those controls (class handler, so it also covers
    /// the calendar popup) — they still change via clicks, arrows and typing.
    /// </summary>
    private static void DampenPickerWheel()
    {
        InputElement.PointerWheelChangedEvent.AddClassHandler<Calendar>(
            (_, e) => e.Handled = true, RoutingStrategies.Tunnel);
        InputElement.PointerWheelChangedEvent.AddClassHandler<NumericUpDown>(
            (_, e) => e.Handled = true, RoutingStrategies.Tunnel);
    }

    // ---------- tray ----------
    private void BuildTray()
    {
        WindowIcon? icon = null;
        try
        {
            using var s = AssetLoader.Open(new Uri("avares://Sayac/Assets/soonish.ico"));
            icon = new WindowIcon(s);
        }
        catch { /* tray still works without a custom icon on some platforms */ }

        _miMain = new NativeMenuItem();
        _miMain.Click += (_, _) => ToggleMain();
        _miMini = new NativeMenuItem();
        _miMini.Click += (_, _) => ToggleMini();
        _miLock = new NativeMenuItem();
        _miLock.Click += (_, _) => ToggleLock();
        _miRecover = new NativeMenuItem();
        _miRecover.Click += (_, _) => OnRecoverMini();
        _miSettings = new NativeMenuItem();
        _miSettings.Click += (_, _) => OpenAppSettings();
        _miExit = new NativeMenuItem();
        _miExit.Click += (_, _) => ExitApp();

        var menu = new NativeMenu();
        menu.Add(_miMain);
        menu.Add(_miMini);
        menu.Add(_miLock);
        menu.Add(_miRecover);
        menu.Add(_miSettings);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(_miExit);

        _tray = new TrayIcon { Icon = icon, ToolTipText = "Sayaç", IsVisible = true, Menu = menu };
        _tray.Clicked += (_, _) => ToggleMain();

        TrayIcon.SetIcons(this, new TrayIcons { _tray });

        UpdateTrayText();
        LocalizationService.Instance.LanguageChanged += UpdateTrayText;
    }

    private void UpdateTrayText()
    {
        var loc = LocalizationService.Instance;
        if (_miMain is not null) _miMain.Header = loc["TrayToggleMain"];
        if (_miMini is not null) _miMini.Header = loc["TrayToggleMini"];
        if (_miLock is not null) _miLock.Header = loc["TrayToggleLock"];
        if (_miRecover is not null) _miRecover.Header = loc["TrayRecoverMini"];
        if (_miSettings is not null) _miSettings.Header = loc["TraySettings"];
        if (_miExit is not null) _miExit.Header = loc["TrayExit"];
    }

    // ---------- window toggles ----------
    private void ToggleMain()
    {
        if (_mainWindow is null) return;
        if (_mainWindow.IsVisible) _mainWindow.Hide();
        else { _mainWindow.Show(); _mainWindow.Activate(); }
    }

    private void ToggleMini()
    {
        if (_miniWindow is null) return;
        if (_miniWindow.IsVisible) { _miniWindow.Hide(); _main.Settings.MiniVisible = false; }
        else { _miniWindow.Show(); _main.Settings.MiniVisible = true; }
    }

    private void ToggleLock() => _main.Settings.MiniLocked = !_main.Settings.MiniLocked;

    /// <summary>Bring the (possibly off-screen / hidden) mini overlay back to a visible corner.</summary>
    private void OnRecoverMini()
    {
        if (_miniWindow is null) return;
        _main.Settings.MiniX = double.NaN;   // clear off-screen position (also triggers save)
        _main.Settings.MiniY = double.NaN;
        if (!_miniWindow.IsVisible)
        {
            _miniWindow.Show();
            _main.Settings.MiniVisible = true;
        }
        _miniWindow.RecoverToDefault();
    }

    private void OnMainClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_exiting) return;
        e.Cancel = true;
        _mainWindow?.Hide();
    }

    private void OpenAppSettings()
    {
        if (_settingsWindow is not null) { _settingsWindow.Activate(); return; }
        var vm = new AppSettingsViewModel(_main.Settings, _hotkeys);
        vm.Changed += () => _main.RequestSave();
        vm.RecoverMiniRequested += OnRecoverMini;
        _settingsWindow = new AppSettingsWindow { DataContext = vm };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void OpenCounterSettings(CounterViewModel counter)
    {
        if (_counterWindow is not null) { try { _counterWindow.Close(); } catch { } }
        _counterWindow = new CounterSettingsWindow { DataContext = new CounterSettingsViewModel(counter) };
        _counterWindow.Closed += (_, _) => _counterWindow = null;
        _counterWindow.Show();
        _counterWindow.Activate();
    }

    private void ExitApp()
    {
        _exiting = true;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    // ---------- minimal message dialog ----------
    private void ShowMessage(string title, string message)
    {
        var ok = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 80
        };
        var dlg = new Window
        {
            Title = title,
            Width = 320,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = Brush.Parse("#111111"),
            Content = new StackPanel
            {
                Margin = new Thickness(18),
                Spacing = 14,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.White
                    },
                    ok
                }
            }
        };
        ok.Click += (_, _) => dlg.Close();
        dlg.Show();
    }
}
