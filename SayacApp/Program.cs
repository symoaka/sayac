using Avalonia;
using System;
using SayacApp.Services;

namespace SayacApp;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Peek the document once (no dispatcher/timer) so we can pick the renderer before
        // Avalonia spins up, then hand it to App so it isn't loaded twice.
        var data = SettingsStore.LoadData();
        App.Preloaded = data;
        BuildAvaloniaApp(data.Settings.PerformanceMode).StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() => BuildAvaloniaApp(false);

    private static AppBuilder BuildAvaloniaApp(bool softwareRendering)
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        if (softwareRendering)
        {
            // Drop the GPU compositor (Metal/ANGLE) for a CPU framebuffer — frees ~90 MB of
            // graphics memory at the cost of more CPU when the screen actually repaints.
            builder = builder
                .With(new AvaloniaNativePlatformOptions
                    { RenderingMode = new[] { AvaloniaNativeRenderingMode.Software } })
                .With(new Win32PlatformOptions
                    { RenderingMode = new[] { Win32RenderingMode.Software } });
        }

        return builder;
    }
}
