[assembly: AvaloniaTestApplication(typeof(BlueHeighliner.NugetFetcher.Tests.TestApplication))]

namespace BlueHeighliner.NugetFetcher.Tests;

public sealed class TestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
