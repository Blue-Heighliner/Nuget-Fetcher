namespace BlueHeighliner.NugetFetcher;

/// <summary>
/// The application entry point.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// Starts the application. Nothing Avalonia-, third-party- or SynchronizationContext-reliant may run
    /// before the app builder is configured, since the platform isn't initialized yet.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Builds the Avalonia configuration. Also used by the visual designer.
    /// </summary>
    /// <returns>The configured app builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions { OverlayPopups = true })
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
